using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public sealed class SpikeBoot : MonoBehaviour
{
    const string Yaml = "- name: David Ben-Gurion\n  portfolio: Prime Minister & Defense\n- name: Eliezer Kaplan\n  portfolio: Finance\n";
    public PanelSettings panelSettings;
    // API correction: Shader.Find("Universal Render Pipeline/Lit") returns null in the player (shader stripped); reference a material asset instead.
    public Material hexMaterial;
    readonly List<CabinetVM> _vms = new List<CabinetVM>();
    Mesh _hex; Material _mat; Volume _dofVolume; bool _dofOn = true; Matrix4x4[] _xf; float _fpsTimer; int _frames;

    void Start()
    {
        // (c) A plain .NET Standard DLL plus YamlDotNet, used from Unity.
        var ministers = SpikeLib.Probe.ParseMinisters(Yaml);
        Debug.Log($"SPIKE c: yaml ok count={ministers.Count} first={ministers[0].Name}");

        // (d) UI Toolkit runtime data binding: 13 cards, each bound to its own view model.
        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        var root = doc.rootVisualElement;
        root.style.flexDirection = FlexDirection.Row; root.style.flexWrap = Wrap.Wrap;
        for (int i = 0; i < 13; i++)
        {
            var vm = new CabinetVM { Name = i < ministers.Count ? ministers[i].Name : $"Minister {i}", Portfolio = "Portfolio", Competence = 5 };
            _vms.Add(vm);
            var card = new VisualElement { dataSource = vm };
            card.style.width = 180; card.style.height = 90; /* 70 clipped the third label */ card.style.backgroundColor = new Color(0.07f, 0.1f, 0.18f, 0.9f);
            card.style.marginLeft = 4; card.style.marginTop = 4;
            foreach (var (prop, path) in new[] { ("text", nameof(CabinetVM.Name)), ("text", nameof(CabinetVM.Portfolio)), ("text", nameof(CabinetVM.Competence)) })
            {
                var label = new Label(); label.style.color = new Color(0.9f, 0.85f, 0.7f);
                label.SetBinding(prop, new DataBinding { dataSourcePath = new PropertyPath(path), bindingMode = BindingMode.ToTarget });
                card.Add(label);
            }
            root.Add(card);
        }
        Invoke(nameof(MutateOne), 2f);

        // (e) URP depth of field: the tilt-shift look.
        var volume = new GameObject("DoF").AddComponent<Volume>(); _dofVolume = volume;
        volume.isGlobal = true; volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        // Gaussian, not Bokeh: Bokeh is physical, so at map-scale distances everything is in focus.
        var dof = volume.profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian); dof.gaussianStart.Override(80f);
        dof.gaussianEnd.Override(160f); dof.gaussianMaxRadius.Override(1.5f);
        Debug.Log("SPIKE e: gaussian dof volume created");

        // Legacy TextMesh labels are used for settlement names in Task 25; check they render under URP.
        var textProbe = new GameObject("LabelProbe").AddComponent<TextMesh>();
        textProbe.text = "Tel Aviv"; textProbe.characterSize = 1f; textProbe.color = Color.black;
        textProbe.transform.position = new Vector3(45, 6, 30); textProbe.transform.rotation = Quaternion.Euler(50, 0, 0);
        Debug.Log("SPIKE f: textmesh label created (check it is visible, not missing or magenta)");
        // Variant: same label with the built-in legacy font assigned explicitly (AddComponent<TextMesh> leaves font null).
        var textProbe2 = new GameObject("LabelProbe2").AddComponent<TextMesh>();
        textProbe2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textProbe2.GetComponent<MeshRenderer>().sharedMaterial = textProbe2.font.material;
        textProbe2.text = "Haifa"; textProbe2.characterSize = 1f; textProbe2.color = Color.black;
        textProbe2.transform.position = new Vector3(45, 6, 45); textProbe2.transform.rotation = Quaternion.Euler(50, 0, 0);
        Debug.Log($"SPIKE f2: textmesh default font={(textProbe.font == null ? "null" : textProbe.font.name)}; explicit-font label 'Haifa' created");

        // (a) Map stress test: 4,000 instanced hex caps (about the hex count of the real map).
        _hex = BuildHexCap(); _mat = hexMaterial;
        _xf = new Matrix4x4[4000];
        for (int i = 0; i < _xf.Length; i++)
        {
            int q = i % 50, r = i / 50;
            var p = new Vector3(Mathf.Sqrt(3f) * (q + r / 2f), Mathf.PerlinNoise(q * 0.1f, r * 0.1f) * 3f, 1.5f * r);
            _xf[i] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one);
        }
        var cam = Camera.main; cam.transform.position = new Vector3(45, 70, -10); cam.transform.rotation = Quaternion.Euler(50, 0, 0); cam.fieldOfView = 35;
        // API correction: a camera made by NewSceneSetup.DefaultGameObjects has URP post-processing OFF, so no Volume effect shows until it is enabled.
        var camData = cam.GetUniversalAdditionalCameraData();
        Debug.Log($"SPIKE e: camera renderPostProcessing was {camData.renderPostProcessing}; enabling");
        camData.renderPostProcessing = true;
    }

    void MutateOne() { _vms[0].Competence = 9; Debug.Log("SPIKE d: mutated vm0 competence to 9 (check card shows 9)"); }

    void Update()
    {
        var rp = new RenderParams(_mat);
        for (int i = 0; i < _xf.Length; i += 1023)
            Graphics.RenderMeshInstanced(rp, _hex, 0, _xf, Mathf.Min(1023, _xf.Length - i), i);
        // Evidence for (e): DoF alternates on/off every 4 s so screenshots can compare.
        bool on = ((int)(Time.unscaledTime / 4f)) % 2 == 0;
        if (on != _dofOn) { _dofOn = on; _dofVolume.enabled = on; Debug.Log($"SPIKE e: dof {(on ? "ON" : "OFF")}"); }
        _frames++; _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 5f) { Debug.Log($"SPIKE a: fps={_frames / _fpsTimer:F1}"); _frames = 0; _fpsTimer = 0; }
    }

    static Mesh BuildHexCap() // top face only: enough to load the instancing path
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int k = 0; k < 6; k++) { float a = Mathf.Deg2Rad * (60 * k - 30); v.Add(new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a))); }
        for (int k = 1; k < 5; k++) { t.Add(0); t.Add(k + 1); t.Add(k); }
        var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
    }
}
