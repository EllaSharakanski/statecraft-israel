using System;
using Unity.Properties;
using UnityEngine.UIElements;

public sealed class CabinetVM : INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
    string _name = "", _portfolio = "";
    int _competence;

    [CreateProperty] public string Name { get => _name; set { _name = value; Notify(nameof(Name)); } }
    [CreateProperty] public string Portfolio { get => _portfolio; set { _portfolio = value; Notify(nameof(Portfolio)); } }
    [CreateProperty] public int Competence { get => _competence; set { _competence = value; Notify(nameof(Competence)); } }

    void Notify(string p) => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(p));
}
