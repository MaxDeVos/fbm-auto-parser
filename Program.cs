using HtmlAgilityPack;

namespace FacebookMarketplaceCarSearchParser;

public class Program
{
    public static void Main(string[] args)
    {
        // DocumentParser parser = new DocumentParser("pages\\wrapping-test.html");

        var text= File.ReadAllText(@"C:\Workspace\CSharp\FacebookMarketplaceCarSearchParser\pages\page.html");
        
        DocumentParser parser = new DocumentParser(text);
        
        List<CarListing> carListings = new();
        var outStr = "";
        
        foreach (var carNode in parser.GetCarNodes())
        {
            var node = new CarListing(carNode);
            outStr += $"{node}\n";
            carListings.Add(node);
        }
        File.WriteAllText(@"C:\Workspace\CSharp\FacebookMarketplaceCarSearchParser\data\cars.csv",outStr);

        List<string> makes = new()
        {
            "Toyota",
            "Honda",
            "Acura",
            "Lexus",
            "Subaru"
        };

        var maxMiles = 150000;

        var filteredListings = "";
        foreach (var carListing in carListings)
        {
            if(!makes.Contains(carListing.Make))
            {
                continue;
            }

            if (carListing.Miles > maxMiles)
            {
                continue;
            }

            filteredListings += $"{carListing}\n";

        }
        
        File.WriteAllText(@"C:\Workspace\CSharp\FacebookMarketplaceCarSearchParser\data\filtered.csv",filteredListings);
        File.WriteAllText(@"C:\Workspace\CSharp\FacebookMarketplaceCarSearchParser\data\cars.csv",outStr);
        
    }
}