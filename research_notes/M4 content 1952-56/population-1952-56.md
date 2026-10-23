# M4 content research: immigration, emigration and population, 1952 to 1956

Research only. Researched 2026-10-23; all pages were opened on that date. Nothing here is a
decision about `content/aliyah.yaml`, `content/population.yaml`, the emigration code or the
calibration test, which stay paused (#102, maintainer comment of 22 October 2026).

**Conventions**
- **[V]** verified on the cited page, which was opened. The CBS volumes here are PDFs, so each
  table was read both as extracted text and on a rendered image of the page, and the two were
  compared before a figure was accepted.
- **[S]** single source, or a search-result summary that was not opened directly.
- **[C]** sources conflict. Both values are given, each with its source.
- **[GAP]** not found.
- A figure marked **computed here** is plain arithmetic on a cited table, not a printed CBS figure.
- An interpretation is marked **our reading** and is not attributed to CBS.
- No quotes are invented. The short quotes below are copied from the cited page.

**Access notes**
- `economy-series-1952-56.md` records that no volume of the CBS Statistical Abstract for these
  years was found online. Two volumes carrying the 1952 to 1956 series were reached for this note:
  the **Statistical Abstract of Israel 2010**, whose historical tables run back to 1948, and
  **Statistical Abstract of Israel No. 14 (1963)**, which is contemporary with the window and is
  published under `publications/DocLib/shnaton_saruk/`. That directory is the same one the repo
  already cites for `shnaton1950_num1.pdf`, `shnaton1951_num2.pdf` and `shnaton1952_num3.pdf`.
- knesset.gov.il (both `fs.knesset.gov.il` and `main.knesset.gov.il`) returns HTTP 247 and a
  JavaScript bot challenge rather than content, which is the same wall recorded in
  `research_notes/M3 content 1950-51/facts.md`. Nothing in this note depends on it.
- `https://www.cbs.gov.il/en/subjects/Pages/Immigration.aspx` answers HTTP 200 but serves the CBS
  "page not found" page. It is not a source and is not cited below.
- The 1963 volume is an old scan and its text layer is unreliable. Three figures in it were
  misread by text extraction and corrected against the rendered page; they are flagged in place.

**Sources**
- **SA 2010 table 4.2**, immigrants by period of immigration and last continent of residence,
  printed page 230.
  https://www.cbs.gov.il/he/publications/doclib/2010/4.%20shnatonimmigration/st04_02.pdf
- **SA 2010 table 2.1**, population by population group, printed page 85.
  https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_01.pdf
- **SA No. 14 (1963) table 2**, sources of increase of the population (estimate), printed page 16.
  https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1963_num14.pdf
- **SA No. 14 (1963) table 1**, marriages, divorces, births, deaths, natural increase, infant
  deaths and stillbirths, by population groups, printed page 65. Same PDF.

---

## 1. Arrivals by year and continent of last residence

SA 2010 table 4.2, p. 230. Whole population. [V]

| Year | Total | Asia | Africa | Europe | America and Oceania | Not known |
|---|---|---|---|---|---|---|
| 1952 | 24,610 | 6,867 | 10,286 | 6,232 | 950 | 275 |
| 1953 | 11,575 | 3,014 | 5,102 | 2,147 | 930 | 382 |
| 1954 | 18,491 | 3,357 | 12,509 | 1,369 | 1,091 | 165 |
| 1955 | 37,528 | 1,432 | 32,815 | 2,065 | 1,155 | 61 |
| 1956 | 56,330 | 3,139 | 45,284 | 6,739 | 1,067 | 101 |

**The continent rows sum to the printed total for every one of the five years**, exactly, with no
residual. Computed here, from the table's own columns.

The table also prints two percentage columns, Asia and Africa against Europe, America and Oceania.
Those reproduce exactly when the share is taken over the total **less** the "not known" column
(1952: 17,153 of 24,335 is 70.5 per cent, as printed; 1955: 34,247 of 37,467 is 91.4 per cent, as
printed). Computed here. This is what fixes which column is which, independently of the
right-to-left column order, and it holds for all five years.

The table's own period subtotals are 54,676 for 1952 to 1954 and 166,492 for 1955 to 1957. [V]

**The 1955 and 1956 surge is African.** Africa rises from 5,102 in 1953 to 32,815 in 1955 and
45,284 in 1956, while Asia falls to 1,432 in 1955. In 1955 Africa alone is 87.4 per cent of all
arrivals (computed here). This is the North African aliyah the issue flags as mattering for the
1955 election and the development towns.

**Finer than continent, by year: [GAP].** SA 2010 table 4.4 gives country of birth, but its
columns for this window are the period 1952 to 1960 as one bucket, not single years. No per-year
country-of-origin table was found. The repo's 1950 to 1951 note records the same gap for its own
window.

## 2. Immigrants per year: the two CBS series disagree

| Year | SA No. 14 (1963) t.2, p. 16 | SA 2010 t. 4.2, p. 230 | difference |
|---|---|---|---|
| 1952 | 23,408 | 24,610 | 1,202 |
| 1953 | 10,388 | 11,575 | 1,187 |
| 1954 | 17,485 | 18,491 | 1,006 |
| 1955 | 36,327 | 37,528 | 1,201 |
| 1956 | 54,996 | 56,330 | 1,334 |

[C], both CBS. Both values are given above and neither is presented as the figure.

The 2010 table's own footnote 1 reads: "Incl. potential immigrants and tourists who changed their
status to immigrants. As of 1970 excl. immigrating citizens". [V]

**Our reading:** that footnote describes a wider count than the 1963 volume's `עולים` column, and
the gap is of the right size and sign to be the potential immigrants and status-changing tourists.
The 1963 volume's own definition of its immigrants column was not read, so this is an inference
about the difference, not a statement CBS makes about it. A reader who needs one series should say
which, rather than mixing them across years.

## 3. Emigration by year

SA No. 14 (1963) table 2, p. 16, whole population. [V]

The volume has no column called emigration. It gives **net movement of residents**, which is
negative in every year of this window, and that is the nearest printed series to the one #102 asks
for.

| Year | Immigrants | Net movement of residents | Net movement of travellers | Migration balance |
|---|---|---|---|---|
| 1952 | 23,408 | minus 13,793 | plus 1,416 | plus 11,031 |
| 1953 | 10,388 | minus 13,178 | plus 1,255 | minus 1,535 |
| 1954 | 17,485 | minus 8,324 | plus 1,785 | plus 10,946 |
| 1955 | 36,327 | minus 7,114 | plus 1,940 | plus 31,153 |
| 1956 | 54,996 | minus 12,410 | plus 512 | plus 43,098 |

For every year, immigrants plus net movement of residents plus net movement of travellers equals
the printed migration balance, exactly. Computed here.

**1953 is the year the migration balance is negative**, at minus 1,535, and the table prints its
migration share of total growth as minus 3.8 per cent against a natural increase share of 103.8
per cent. [V] This supports the statement in #102 that in 1953 emigration roughly matched
immigration; on this table it slightly exceeded it.

**Two text-extraction corrections.** The extracted text gave the 1953 migration balance as minus
1,533 and the 1955 migration balance as plus 31,133. On the rendered page both read minus 1,535
and plus 31,153, and only those values satisfy the row arithmetic. The printed figures are used.

**What this series is not.** Net movement of residents is a net figure, so it is not a count of
people who left, and it is not split by destination or by year of arrival. A gross emigration
series for these years was not found. [GAP]

## 4. Population at the end of each year

Two independent CBS tables, agreeing. [V]

| Year | SA No. 14 (1963) t.2, p. 16 | SA 2010 t. 2.1, p. 85 (thousands) | average population (thousands) |
|---|---|---|---|
| 1952 | 1,629,519 | 1,629.5 | 1,606.2 |
| 1953 | 1,669,417 | 1,669.4 | 1,650.3 |
| 1954 | 1,717,814 | 1,717.8 | 1,689.5 |
| 1955 | 1,789,075 | 1,789.1 | 1,750.4 |
| 1956 | 1,872,390 | 1,872.4 | 1,828.4 |

The 1963 volume's figures round to the 2010 volume's figures in every year. Computed here.

In the 1963 table, each year's population at the beginning of the period equals the previous
year's population at the end, and population at the beginning plus total increase equals
population at the end, in every year of the window. Computed here.

**One misprint in the source.** The 1952 row of SA No. 14 table 2 prints the population at the end
of the period as 1,629,319 in the extracted text. The rendered page reads **1,629,519**, which is
also what the 1953 row gives as its population at the beginning of the period, what the row's own
arithmetic requires (1,577,825 plus 51,694), and what SA 2010 table 2.1 gives as 1,629.5 thousand.
The note uses 1,629,519.

SA 2010 table 2.1 also splits each year into Jews, Arabs and others. End of 1952 is 1,450.2
thousand Jews and 179.3 thousand Arabs; end of 1956 is 1,667.5 thousand Jews and 204.9 thousand
Arabs. [V]

## 5. Natural increase: the two tables of the 1963 volume disagree

| Year | table 2, p. 16 | table 1, p. 65 | difference | births (p. 65) | deaths (p. 65) |
|---|---|---|---|---|---|
| 1952 | 40,663 | 40,890 | 227 | 52,556 | 11,666 |
| 1953 | 41,433 | 41,636 | 203 | 52,552 | 10,916 |
| 1954 | 37,451 | 37,623 | 172 | 48,951 | 11,328 |
| 1955 | 40,108 | 40,154 | 46 | 50,686 | 10,532 |
| 1956 | 40,217 | 40,262 | 45 | 52,287 | 12,025 |

[C], and both are in the same volume. On p. 65, births minus deaths equals the printed natural
increase in every year, exactly. Computed here.

Table 1 carries footnote 1: "Excluding all data for the Bedouin in the Negev. As from 1957
marriages and divorces of Negev Bedouin are included. Registration of deaths among non-Jews is
incomplete." [V] Table 2 is headed "Sources of Increase of the Population (Estimate)". [V]

**Our reading:** the p. 65 series is vital registration with a stated coverage gap, and the p. 16
series is a population estimate, so the two are not meant to be the same number. The difference
narrowing from 227 in 1952 to 45 in 1956 is consistent with coverage improving, but no CBS page
read for this note says that, and the direction of the gap is the opposite of what excluding
people from registration alone would produce. Treat the pair as a range, not as one figure with an
error.

## 6. Natural increase rate

**Computed here, not a printed CBS figure.** Natural increase divided by the average population of
the same year (SA 2010 table 2.1, p. 85), per 1,000.

| Year | using table 2, p. 16 | using table 1, p. 65 |
|---|---|---|
| 1952 | 25.3 | 25.5 |
| 1953 | 25.1 | 25.2 |
| 1954 | 22.2 | 22.3 |
| 1955 | 22.9 | 22.9 |
| 1956 | 22.0 | 22.0 |

The two bases agree to about a tenth, so the choice between them does not matter at this precision.

**A caution about the printed rate column.** SA No. 14 table 2 has a column headed "Rate of
Increase", reading 32.8 for 1952, 24.5 for 1953, 29.0 for 1954, 41.5 for 1955 and 46.6 for 1956.
[V] Those are the rate of **total** increase, which includes migration, not the natural increase
rate, and the 1955 and 1956 values are far above the natural rate because those are the heavy
arrival years. They should not be read as the natural increase rate.

The sim's current `natural_increase_per_year` is 0.0169, that is 16.9 per 1,000. Every year in this
window computes higher than that, between 22.0 and 25.5. What, if anything, follows for the rule
value belongs to the paused half of #102 and is not proposed here.

## 7. Gaps

- Per-year arrivals by country of origin, finer than continent. [GAP] See section 1.
- A gross emigration series, as opposed to net movement of residents. [GAP] See section 3.
- The 1963 volume's own definition of its immigrants column, which would settle section 2 rather
  than leaving it as our reading. [GAP]

## 8. Noted while sourcing, outside this issue

`research_notes/M3 content 1950-51/facts.md` records "[GAP] The CBS 'sources of population growth'
table was not located (tried st02_04/05, both 404)" and "No natural-increase figure from CBS". The
table exists at
https://www.cbs.gov.il/he/publications/doclib/2010/2.%20shnatonpopulation/st02_03.pdf,
though for the 1950s it is grouped by period rather than by year. SA No. 14 (1963) table 2, p. 16,
does carry 1950 and 1951 as single years: natural increase 34,482 in 1950 and 40,322 in 1951, with
migration balance plus 161,741 and plus 167,409. [V] That is a change to another note and is not
made here.
