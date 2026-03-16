# Insurance Policy Portfolio Analyzer

This is a personal learning project I made to practice Power BI. I wanted to work with something more interesting than the default sample datasets, so I wrote a Python script that generates a fake but realistic Dutch insurance portfolio.

The idea is simple: run the script once, get a bunch of CSV files, load them into Power BI, and start building dashboards.

---

## What it generates

The script creates 5 CSV files in a `/data` folder:

| File | What's in it | ~Rows |
|------|-------------|-------|
| `agents.csv` | The insurance agents (name, region, branch office, experience) | 25 |
| `policyholders.csv` | The customers (name, age, city, risk profile) | 500 |
| `policies.csv` | The actual policies (type, premium, coverage, status) | 800 |
| `payments.csv` | Monthly premium payments per policy | ~5000 |
| `renewals.csv` | Renewal offers and whether the customer accepted | 300 |

All the foreign keys are consistent, so you can relate the tables to each other in Power BI without problems.

The data uses Dutch names, cities, and provinces thanks to the Faker library with the `nl_NL` locale. Date ranges go from 2021 to 2025.

---

## How to run it

**1. Install dependencies**

```bash
pip install -r requirements.txt
```

**2. Run the script**

```bash
python generate_data.py
```

That's it. A `/data` folder will appear with all 5 CSV files inside.

---

## How to import into Power BI

1. Open Power BI Desktop
2. Click **Home → Get Data → Text/CSV**
3. Navigate to the `/data` folder and load all 5 files one by one
4. After loading, go to **Transform Data** (Power Query) and check that the column types look right — dates should be detected as Date, numbers as Decimal or Whole Number
5. Close and apply

**Setting up the relationships:**

Go to the **Model view** (the icon that looks like a diagram on the left sidebar) and make sure these relationships exist:

- `policies[policyholder_id]` → `policyholders[policyholder_id]`
- `policies[agent_id]` → `agents[agent_id]`
- `payments[policy_id]` → `policies[policy_id]`
- `renewals[policy_id]` → `policies[policy_id]`

Power BI might auto-detect some of these. If not, you can drag and drop the columns in Model view to create the relationships manually.

---

## Some dashboard ideas

Things I'm planning to build with this data:

- **Portfolio overview** — total premiums, active vs expired vs cancelled policies, split by type
- **Agent performance** — which agents bring in the most premium revenue, how many policies per agent
- **Payment health** — on-time vs late vs missed payments over time, by policy type or risk profile
- **Renewal funnel** — how many offers were sent, how many were accepted, average premium increase
- **Geographic breakdown** — policies and revenue by province (map visual)
- **Customer risk analysis** — age distribution, risk profiles, average coverage amounts

---

## Notes

- Every time you run the script you get the same data (seeds are fixed at 42) — useful if you want to share the project and have the same numbers
- If you want different data, change the `random.seed(42)` lines at the top of the script
- The data is completely fictional — any resemblance to real people or policies is just Faker being good at its job
