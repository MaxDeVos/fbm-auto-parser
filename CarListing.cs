using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace FacebookMarketplaceCarSearchParser;

public class CarListing
{
    public decimal Price { get; }
    public string Location { get; }
    public string Make { get; }
    public string Model { get; }
    public string? Trim { get; } = "";
    public int? Year { get; } = 0;
    public int? Miles { get; }
    public string Url { get; }

    public CarListing(HtmlNode carNode)
    {
        Url = carNode.Attributes["href"].Value;
        var dataStrings = ExtractDataStrings(carNode);

        if (dataStrings == null)
        {
            throw new Exception();
        }
        
        Price = decimal.Parse(dataStrings[0].Replace("$", "").Replace(",", ""));
        Location = dataStrings[2].Replace(",","");
        if (dataStrings[3] != "")
        {
            Miles = int.Parse(Regex.Match(dataStrings[3], @"(\d{0,9}K?)").Groups[0].Value.Replace("K", "000"));
        }

        Year = int.Parse(dataStrings[1].Split(" ")[0]);
        var description = dataStrings[1].Replace($"{Year} ","");

        Make = description.Split(" ")[0];
        description = description.Replace($"{Make} ", "");
        Console.WriteLine(description);
        
        Model = description.Split(" ")[0];
        description = description.Replace($"{Model} ", "");
        if (description != "")
        {
            Trim = description;
        }
    }

    /**
     * length = 4
     * 0 = cost
     * 1 = description
     * 2 = location
     * 3 = miles
     */
    private List<string>? ExtractDataStrings(HtmlNode carNode)
    {
        List<HtmlNode> carDataNodes = new();
        List<string> dataStrings = new();
        foreach (var descendant in carNode.Descendants())
        {
            if (descendant.Attributes.Contains("dir") && descendant.Attributes["dir"].Value == "auto")
            {
                carDataNodes.Add(descendant);
            }
        }

        // remove old price node
        if (carDataNodes.Count == 5)
        {
            carDataNodes.Remove(carDataNodes[1]);
        }
        
        foreach (var dataNode in carDataNodes)
        {
            var data = Regex.Match(dataNode.InnerHtml, @"(?:<.*?>)*(?<data>[^<>]*)(?:<?.*?>)*").Groups["data"].Value;
            dataStrings.Add(data);
        }
        
        if (dataStrings.Count != 4)
        {
            Console.WriteLine("====================================");
            Console.WriteLine("SOMETHING FUCKED FOR CAR ENTRY: ");
            Console.WriteLine(carNode.OuterHtml);
            Console.WriteLine("----------------");
            foreach (var entry in dataStrings)
            {
                Console.WriteLine(entry);
            }
            Console.WriteLine("====================================");
            return null;
        }

        return dataStrings;
    }

    public override string ToString()
    {
        return $"{Year},{Make},{Model},{Trim},{Miles},{Price},{Location},{Url}";
    }
}