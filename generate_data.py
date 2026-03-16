import os
import random
from datetime import date, timedelta

import numpy as np
import pandas as pd
from faker import Faker

# set seeds so i get the same data every time i run it
fake = Faker("nl_NL")
Faker.seed(42)
random.seed(42)
np.random.seed(42)

os.makedirs("data", exist_ok=True)

# --- some lookup lists i use across multiple generators ---

PROVINCES = [
    "Noord-Holland",
    "Zuid-Holland",
    "Utrecht",
    "Noord-Brabant",
    "Gelderland",
    "Overijssel",
    "Groningen",
    "Friesland",
    "Drenthe",
    "Zeeland",
    "Limburg",
    "Flevoland",
]

# using a fixed list because fake.city() sometimes gives weird results with nl_NL
DUTCH_CITIES = [
    "Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven",
    "Groningen", "Tilburg", "Almere", "Breda", "Nijmegen",
    "Apeldoorn", "Haarlem", "Arnhem", "Enschede", "Zwolle",
    "Haarlemmermeer", "Zoetermeer", "Leiden", "Maastricht", "Dordrecht",
    "Ede", "Westland", "Delft", "Emmen", "Deventer",
    "Sittard-Geleen", "Helmond", "Venlo", "Alkmaar", "Amersfoort",
]

BRANCH_CITIES = [
    "Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven",
    "Groningen", "Tilburg", "Arnhem", "Haarlem", "Maastricht",
    "Zwolle", "Breda", "Leiden", "Alkmaar", "Amersfoort",
]


def random_date(start: date, end: date) -> date:
    delta = (end - start).days
    return start + timedelta(days=random.randint(0, delta))


# -------------------------------------------------------------------------
# AGENTS
# -------------------------------------------------------------------------

def generate_agents(n=25):
    print(f"Generating {n} agents...")
    regions = ["Noord", "Zuid", "Oost", "West", "Midden"]
    rows = []

    for i in range(1, n + 1):
        rows.append({
            "agent_id": f"AG{i:03d}",
            "full_name": fake.name(),
            "region": random.choice(regions),
            "branch_office": random.choice(BRANCH_CITIES),
            "years_experience": random.randint(1, 30),
        })

    df = pd.DataFrame(rows)
    df.to_csv("data/agents.csv", index=False)
    print(f"  saved {len(df)} rows -> data/agents.csv")
    return df


# -------------------------------------------------------------------------
# POLICYHOLDERS
# -------------------------------------------------------------------------

def generate_policyholders(n=500):
    print(f"Generating {n} policyholders...")
    rows = []

    for i in range(1, n + 1):
        gender = random.choice(["M", "F"])
        first_name = fake.first_name_male() if gender == "M" else fake.first_name_female()

        rows.append({
            "policyholder_id": f"PH{i:04d}",
            "first_name": first_name,
            "last_name": fake.last_name(),
            "age": random.randint(18, 80),
            "gender": gender,
            "city": random.choice(DUTCH_CITIES),
            "province": random.choice(PROVINCES),
            "risk_profile": random.choices(
                ["Low", "Medium", "High"], weights=[0.40, 0.40, 0.20]
            )[0],
            "customer_since": random_date(date(2015, 1, 1), date(2022, 12, 31)).strftime("%Y-%m-%d"),
        })

    df = pd.DataFrame(rows)
    df.to_csv("data/policyholders.csv", index=False)
    print(f"  saved {len(df)} rows -> data/policyholders.csv")
    return df


# -------------------------------------------------------------------------
# POLICIES
# -------------------------------------------------------------------------

def generate_policies(policyholder_ids, agent_ids, n=800):
    print(f"Generating {n} policies...")

    # rough premium and coverage ranges per policy type
    premium_ranges = {
        "Auto":   (80,  250),
        "Home":   (40,  150),
        "Life":   (50,  300),
        "Health": (100, 200),
    }
    coverage_ranges = {
        "Auto":   (10_000,  100_000),
        "Home":   (150_000, 600_000),
        "Life":   (50_000,  500_000),
        "Health": (25_000,  200_000),
    }

    rows = []
    for i in range(1, n + 1):
        p_type = random.choice(list(premium_ranges.keys()))
        start = random_date(date(2021, 1, 1), date(2024, 6, 30))
        end = start + timedelta(days=365)

        # figure out status based on whether the policy has already ended
        if end < date(2025, 1, 1):
            status = random.choices(
                ["Expired", "Cancelled", "Renewed"],
                weights=[0.40, 0.20, 0.40],
            )[0]
        else:
            status = "Active"

        premium = round(random.uniform(*premium_ranges[p_type]), 2)

        # round coverage to nearest 1000 so it looks a bit more realistic
        coverage = random.randint(*coverage_ranges[p_type])
        coverage = round(coverage / 1000) * 1000

        # policy number format that looks like a Dutch insurer might use
        policy_num = f"NL-{start.year}-{random.randint(100_000, 999_999)}"

        rows.append({
            "policy_id": f"POL{i:04d}",
            "policy_number": policy_num,
            "type": p_type,
            "start_date": start.strftime("%Y-%m-%d"),
            "end_date": end.strftime("%Y-%m-%d"),
            "premium_monthly": premium,
            "coverage_amount": coverage,
            "status": status,
            "policyholder_id": random.choice(policyholder_ids),
            "agent_id": random.choice(agent_ids),
        })

    df = pd.DataFrame(rows)
    df.to_csv("data/policies.csv", index=False)
    print(f"  saved {len(df)} rows -> data/policies.csv")
    return df


# -------------------------------------------------------------------------
# PAYMENTS
# -------------------------------------------------------------------------

def generate_payments(policies_df):
    print("Generating payments...")
    PAYMENT_CUTOFF = date(2025, 3, 16)  # today-ish, so active policies have recent data
    methods = ["iDEAL", "Bank Transfer", "Direct Debit"]
    rows = []
    payment_id = 1

    for _, policy in policies_df.iterrows():
        start = date.fromisoformat(policy["start_date"])
        end = min(date.fromisoformat(policy["end_date"]), PAYMENT_CUTOFF)

        if start > end:
            continue

        # generate monthly payment records
        current = start
        while current <= end:
            pay_status = random.choices(
                ["On-time", "Late", "Missed"],
                weights=[0.82, 0.12, 0.06],
            )[0]

            if pay_status == "On-time":
                pay_date = current + timedelta(days=random.randint(0, 3))
            elif pay_status == "Late":
                pay_date = current + timedelta(days=random.randint(5, 28))
            else:
                # missed payment - record the due date, amount is 0
                pay_date = current

            amount = round(policy["premium_monthly"], 2) if pay_status != "Missed" else 0.00

            rows.append({
                "payment_id": f"PAY{payment_id:05d}",
                "policy_id": policy["policy_id"],
                "payment_date": pay_date.strftime("%Y-%m-%d"),
                "amount_paid": amount,
                "payment_method": random.choice(methods),
                "status": pay_status,
            })

            payment_id += 1
            current += timedelta(days=30)

    df = pd.DataFrame(rows)

    # sample down to ~5000 rows if we ended up generating way more
    # i keep the sort so the dates still make sense in power bi
    if len(df) > 5500:
        df = (
            df.sample(5000, random_state=42)
            .sort_values(["policy_id", "payment_date"])
            .reset_index(drop=True)
        )
        # re-number the payment ids after sampling
        df["payment_id"] = [f"PAY{i+1:05d}" for i in range(len(df))]

    df.to_csv("data/payments.csv", index=False)
    print(f"  saved {len(df)} rows -> data/payments.csv")
    return df


# -------------------------------------------------------------------------
# RENEWALS
# -------------------------------------------------------------------------

def generate_renewals(policies_df, n=300):
    print(f"Generating {n} renewals...")

    # renewals only make sense for policies that have ended or are about to end
    eligible = policies_df[policies_df["status"].isin(["Expired", "Renewed", "Cancelled"])].copy()
    sample = eligible.sample(min(n, len(eligible)), random_state=42)

    rows = []
    for i, (_, policy) in enumerate(sample.iterrows(), start=1):
        expiry = date.fromisoformat(policy["end_date"])
        offer_sent = expiry - timedelta(days=random.randint(30, 60))

        # outcome probabilities depend on why the policy ended
        if policy["status"] == "Renewed":
            outcome_weights = [0.70, 0.20, 0.10]  # Accepted / Rejected / No Response
        elif policy["status"] == "Expired":
            outcome_weights = [0.20, 0.30, 0.50]
        else:  # Cancelled
            outcome_weights = [0.10, 0.60, 0.30]

        outcome = random.choices(["Accepted", "Rejected", "No Response"], weights=outcome_weights)[0]

        if outcome != "No Response":
            response_date = (offer_sent + timedelta(days=random.randint(3, 25))).strftime("%Y-%m-%d")
        else:
            response_date = ""

        # new premium is a bit higher than the old one (price indexation etc.)
        new_premium = round(policy["premium_monthly"] * random.uniform(1.02, 1.15), 2)

        rows.append({
            "renewal_id": f"REN{i:04d}",
            "policy_id": policy["policy_id"],
            "expiry_date": expiry.strftime("%Y-%m-%d"),
            "offer_sent_date": offer_sent.strftime("%Y-%m-%d"),
            "response_date": response_date,
            "outcome": outcome,
            "new_premium": new_premium,
        })

    df = pd.DataFrame(rows)
    df.to_csv("data/renewals.csv", index=False)
    print(f"  saved {len(df)} rows -> data/renewals.csv")
    return df


# -------------------------------------------------------------------------
# MAIN
# -------------------------------------------------------------------------

if __name__ == "__main__":
    print("=" * 50)
    print("Insurance Portfolio Data Generator")
    print("=" * 50)

    agents_df = generate_agents(n=25)
    policyholders_df = generate_policyholders(n=500)

    policies_df = generate_policies(
        policyholder_ids=policyholders_df["policyholder_id"].tolist(),
        agent_ids=agents_df["agent_id"].tolist(),
        n=800,
    )

    payments_df = generate_payments(policies_df)
    renewals_df = generate_renewals(policies_df, n=300)

    print("=" * 50)
    print("Done! All CSV files are in the /data folder.")
    print(f"  agents.csv        : {len(agents_df):>5} rows")
    print(f"  policyholders.csv : {len(policyholders_df):>5} rows")
    print(f"  policies.csv      : {len(policies_df):>5} rows")
    print(f"  payments.csv      : {len(payments_df):>5} rows")
    print(f"  renewals.csv      : {len(renewals_df):>5} rows")
