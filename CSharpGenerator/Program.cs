// -------------------------------------------------------------------------
// Insurance Policy Portfolio Generator - C# version
// -------------------------------------------------------------------------
// I already built this in Python (generate_data.py) and wanted to try
// rebuilding it in C# as extra practice. The output is the same 5 CSV files
// so you can use either script for Power BI.
//
// To run this:
//   cd CSharpGenerator
//   dotnet run
//
// Dependencies:
//   dotnet add package Bogus
// -------------------------------------------------------------------------

using Bogus;
using Bogus.DataSets;

Console.WriteLine("==================================================");
Console.WriteLine("Insurance Portfolio Data Generator (C# version)");
Console.WriteLine("==================================================");

// Bogus is the C# equivalent of Python's Faker library
// "nl" gives Dutch names, cities etc. - same as Faker("nl_NL") in Python
var faker = new Faker("nl");

// fix the seed so I get the same data every run
// in Python I used random.seed(42) - this is the C# way
Randomizer.Seed = new Random(42);

// create the output folder - won't throw an error if it already exists
Directory.CreateDirectory("data");

// -------------------------------------------------------------------------
// LOOKUP DATA
// these are just arrays I reuse in multiple places
// -------------------------------------------------------------------------

var provinces = new[]
{
    "Noord-Holland", "Zuid-Holland", "Utrecht", "Noord-Brabant",
    "Gelderland", "Overijssel", "Groningen", "Friesland",
    "Drenthe", "Zeeland", "Limburg", "Flevoland"
};

// using a fixed list because faker.Address.City() sometimes gives odd results
var dutchCities = new[]
{
    "Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven",
    "Groningen", "Tilburg", "Almere", "Breda", "Nijmegen",
    "Apeldoorn", "Haarlem", "Arnhem", "Enschede", "Zwolle",
    "Haarlemmermeer", "Zoetermeer", "Leiden", "Maastricht", "Dordrecht",
    "Ede", "Westland", "Delft", "Emmen", "Deventer",
    "Sittard-Geleen", "Helmond", "Venlo", "Alkmaar", "Amersfoort"
};

var branchCities = new[]
{
    "Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven",
    "Groningen", "Tilburg", "Arnhem", "Haarlem", "Maastricht",
    "Zwolle", "Breda", "Leiden", "Alkmaar", "Amersfoort"
};

var policyTypes    = new[] { "Auto", "Home", "Life", "Health" };
var paymentMethods = new[] { "iDEAL", "Bank Transfer", "Direct Debit" };
var regions        = new[] { "Noord", "Zuid", "Oost", "West", "Midden" };

// repeat Low and Medium to get 40/40/20 weighting - same trick I used in Python
var riskProfiles = new[] { "Low", "Low", "Medium", "Medium", "High" };

// premium and coverage ranges per policy type
// C# tuples are nice for this - cleaner than a 2D array
var premiumRanges = new Dictionary<string, (double Min, double Max)>
{
    { "Auto",   (80,  250) },
    { "Home",   (40,  150) },
    { "Life",   (50,  300) },
    { "Health", (100, 200) }
};

var coverageRanges = new Dictionary<string, (int Min, int Max)>
{
    { "Auto",   (10_000,  100_000) },
    { "Home",   (150_000, 600_000) },
    { "Life",   (50_000,  500_000) },
    { "Health", (25_000,  200_000) }
};

// -------------------------------------------------------------------------
// HELPER FUNCTIONS
// I put these up here so I can use them anywhere below
// (in C# local functions like this are only visible in the same method/file)
// -------------------------------------------------------------------------

// C# doesn't have random.choice() built in like Python does
// this is my workaround - just pick a random index
T Pick<T>(T[] items) => items[faker.Random.Int(0, items.Length - 1)];

// generate a random date between two DateTimes
// DateTime maths in C# is a bit more verbose than Python's timedelta
DateTime RandomDate(DateTime start, DateTime end)
{
    int rangeDays = (end - start).Days;
    return start.AddDays(faker.Random.Int(0, rangeDays));
}

// wrap a CSV value in quotes if it contains a comma
// needed because Dutch names can have tussenvoegsels like "van der Berg"
// without this, those commas would break the CSV columns
string QuoteCsv(string value)
{
    if (value.Contains(',') || value.Contains('"'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}

// -------------------------------------------------------------------------
// AGENTS
// -------------------------------------------------------------------------

Console.WriteLine("Generating agents...");

// "using var" means C# automatically closes the file when done
// same idea as Python's "with open(...) as f"
using (var writer = new StreamWriter("data/agents.csv"))
{
    writer.WriteLine("agent_id,full_name,region,branch_office,years_experience");

    for (int i = 1; i <= 25; i++)
    {
        // $"..." is string interpolation in C# - same as f"..." in Python
        // D3 means pad the number with zeros to 3 digits e.g. 001, 002 etc.
        var agentId    = $"AG{i:D3}";
        var fullName   = faker.Name.FullName();
        var region     = Pick(regions);
        var branch     = Pick(branchCities);
        var experience = faker.Random.Int(1, 30);

        writer.WriteLine($"{agentId},{QuoteCsv(fullName)},{region},{branch},{experience}");
    }
}

Console.WriteLine("  saved 25 rows -> data/agents.csv");

// -------------------------------------------------------------------------
// POLICYHOLDERS
// -------------------------------------------------------------------------

Console.WriteLine("Generating policyholders...");

// I need the IDs later when generating policies, so I keep a list
var policyholderIds = new List<string>();

using (var writer = new StreamWriter("data/policyholders.csv"))
{
    writer.WriteLine("policyholder_id,first_name,last_name,age,gender,city,province,risk_profile,customer_since");

    for (int i = 1; i <= 500; i++)
    {
        var id = $"PH{i:D4}";
        policyholderIds.Add(id);

        // pick gender first so the first name actually matches
        var gender    = faker.Random.Bool() ? "M" : "F";
        var firstName = gender == "M"
            ? faker.Name.FirstName(Name.Gender.Male)
            : faker.Name.FirstName(Name.Gender.Female);

        var lastName     = faker.Name.LastName();
        var age          = faker.Random.Int(18, 80);
        var city         = Pick(dutchCities);
        var province     = Pick(provinces);
        var risk         = Pick(riskProfiles);
        var customerSince = RandomDate(new DateTime(2015, 1, 1), new DateTime(2022, 12, 31))
                            .ToString("yyyy-MM-dd");

        writer.WriteLine($"{id},{QuoteCsv(firstName)},{QuoteCsv(lastName)},{age},{gender},{city},{province},{risk},{customerSince}");
    }
}

Console.WriteLine("  saved 500 rows -> data/policyholders.csv");

// -------------------------------------------------------------------------
// POLICIES
// -------------------------------------------------------------------------

Console.WriteLine("Generating policies...");

// I need to remember each policy's details for the payments and renewals below
// Dictionaries in C# are like Python dicts
var policyIds       = new List<string>();
var policyPremiums  = new Dictionary<string, double>();
var policyStatuses  = new Dictionary<string, string>();
var policyStartDates = new Dictionary<string, DateTime>();
var policyEndDates  = new Dictionary<string, DateTime>();

// build agent ID list - just AG001 to AG025
// Enumerable.Range is like Python's range()
var agentIds = Enumerable.Range(1, 25).Select(i => $"AG{i:D3}").ToArray();

using (var writer = new StreamWriter("data/policies.csv"))
{
    writer.WriteLine("policy_id,policy_number,type,start_date,end_date,premium_monthly,coverage_amount,status,policyholder_id,agent_id");

    for (int i = 1; i <= 800; i++)
    {
        var policyId = $"POL{i:D4}";
        policyIds.Add(policyId);

        var type      = Pick(policyTypes);
        var startDate = RandomDate(new DateTime(2021, 1, 1), new DateTime(2024, 6, 30));
        var endDate   = startDate.AddDays(365);

        // decide on status based on whether the policy has already ended
        string status;
        if (endDate < new DateTime(2025, 1, 1))
        {
            // policy is over - figure out why it ended
            // the extra "Expired" and "Renewed" entries make them more likely (same as Python weights)
            status = Pick(new[] { "Expired", "Expired", "Cancelled", "Renewed", "Renewed" });
        }
        else
        {
            status = "Active";
        }

        var premRange = premiumRanges[type];
        var premium   = Math.Round(faker.Random.Double(premRange.Min, premRange.Max), 2);

        // round coverage to nearest 1000 so it looks realistic
        var covRange = coverageRanges[type];
        var coverage = (faker.Random.Int(covRange.Min, covRange.Max) / 1000) * 1000;

        var policyNumber = $"NL-{startDate.Year}-{faker.Random.Int(100_000, 999_999)}";
        var holderId     = Pick(policyholderIds.ToArray());
        var agentId      = Pick(agentIds);

        // save details for later - I reference these when building payments and renewals
        policyPremiums[policyId]   = premium;
        policyStatuses[policyId]   = status;
        policyStartDates[policyId] = startDate;
        policyEndDates[policyId]   = endDate;

        writer.WriteLine($"{policyId},{policyNumber},{type},{startDate:yyyy-MM-dd},{endDate:yyyy-MM-dd},{premium},{coverage},{status},{holderId},{agentId}");
    }
}

Console.WriteLine("  saved 800 rows -> data/policies.csv");

// -------------------------------------------------------------------------
// PAYMENTS
// -------------------------------------------------------------------------

Console.WriteLine("Generating payments...");

// collect all payment rows as strings first, then sample down if there are too many
// (800 policies x 12 months = ~9600 payments, but we only want ~5000)
var allPaymentRows = new List<string>();

var paymentCutoff = new DateTime(2025, 3, 16); // don't generate payments beyond today-ish

foreach (var policyId in policyIds)
{
    var startDate = policyStartDates[policyId];
    var endDate   = policyEndDates[policyId];
    var premium   = policyPremiums[policyId];

    // only generate payments up to the cutoff date (or policy end, whichever is earlier)
    var cutoff  = endDate < paymentCutoff ? endDate : paymentCutoff;
    var current = startDate;

    while (current <= cutoff)
    {
        // weighted random for payment status: 82% on-time, 12% late, 6% missed
        var roll = faker.Random.Double();
        string payStatus;
        if      (roll < 0.82) payStatus = "On-time";
        else if (roll < 0.94) payStatus = "Late";
        else                  payStatus = "Missed";

        DateTime payDate;
        if      (payStatus == "On-time") payDate = current.AddDays(faker.Random.Int(0, 3));
        else if (payStatus == "Late")    payDate = current.AddDays(faker.Random.Int(5, 28));
        else                             payDate = current; // missed: record the due date, amount = 0

        var amount = payStatus == "Missed" ? 0.00 : Math.Round(premium, 2);
        var method = Pick(paymentMethods);

        // using a placeholder for payment_id - I'll replace it after sampling
        allPaymentRows.Add($"PLACEHOLDER,{policyId},{payDate:yyyy-MM-dd},{amount},{method},{payStatus}");

        current = current.AddDays(30); // next monthly payment
    }
}

// sample down to 5000 if needed - shuffle and take the first 5000
// in Python I used df.sample() - here I'm just shuffling the list
if (allPaymentRows.Count > 5500)
{
    allPaymentRows = faker.Random.Shuffle(allPaymentRows).Take(5000).ToList();
    allPaymentRows.Sort(); // sort by policy id so dates look sensible in Power BI
}

using (var writer = new StreamWriter("data/payments.csv"))
{
    writer.WriteLine("payment_id,policy_id,payment_date,amount_paid,payment_method,status");

    for (int i = 0; i < allPaymentRows.Count; i++)
    {
        // swap the placeholder out for the real sequential ID
        var row = allPaymentRows[i].Replace("PLACEHOLDER", $"PAY{i + 1:D5}");
        writer.WriteLine(row);
    }
}

Console.WriteLine($"  saved {allPaymentRows.Count} rows -> data/payments.csv");

// -------------------------------------------------------------------------
// RENEWALS
// -------------------------------------------------------------------------

Console.WriteLine("Generating renewals...");

// renewals only apply to policies that have ended (not Active ones)
var eligibleForRenewal = policyIds
    .Where(id => policyStatuses[id] != "Active")
    .ToList();

// shuffle and take 300 - same as Python's df.sample(300)
var renewalPolicies = faker.Random.Shuffle(eligibleForRenewal).Take(300).ToList();

using (var writer = new StreamWriter("data/renewals.csv"))
{
    writer.WriteLine("renewal_id,policy_id,expiry_date,offer_sent_date,response_date,outcome,new_premium");

    for (int i = 0; i < renewalPolicies.Count; i++)
    {
        var policyId  = renewalPolicies[i];
        var expiry    = policyEndDates[policyId];
        var offerSent = expiry.AddDays(-faker.Random.Int(30, 60));
        var status    = policyStatuses[policyId];

        // outcome probabilities depend on why the policy ended
        // same logic as the Python version
        var roll = faker.Random.Double();
        string outcome;
        if (status == "Renewed")
            outcome = roll < 0.70 ? "Accepted" : roll < 0.90 ? "Rejected" : "No Response";
        else if (status == "Expired")
            outcome = roll < 0.20 ? "Accepted" : roll < 0.50 ? "Rejected" : "No Response";
        else // Cancelled
            outcome = roll < 0.10 ? "Accepted" : roll < 0.70 ? "Rejected" : "No Response";

        // only set a response date if the customer actually responded
        var responseDate = outcome != "No Response"
            ? offerSent.AddDays(faker.Random.Int(3, 25)).ToString("yyyy-MM-dd")
            : "";

        // new premium is a bit higher due to price indexation etc.
        var newPremium = Math.Round(policyPremiums[policyId] * faker.Random.Double(1.02, 1.15), 2);
        var renewalId  = $"REN{i + 1:D4}";

        writer.WriteLine($"{renewalId},{policyId},{expiry:yyyy-MM-dd},{offerSent:yyyy-MM-dd},{responseDate},{outcome},{newPremium}");
    }
}

Console.WriteLine($"  saved {renewalPolicies.Count} rows -> data/renewals.csv");

// -------------------------------------------------------------------------
// DONE
// -------------------------------------------------------------------------

Console.WriteLine("==================================================");
Console.WriteLine("Done! All CSV files are in the /data folder.");
Console.WriteLine("==================================================");
