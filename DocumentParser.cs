using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace FacebookMarketplaceCarSearchParser;

public class DocumentParser
{
    
    private List<string> _tagsToRemove = new()
    {
        "head",
        "script",
        "svg",
        "#comment",
    };

    private List<string> _attributesToRemove = new()
    {
        "style",
        "data-visualcompletion"
    };

    private bool docChanged = false;
    private HtmlDocument doc;
    
    private List<HtmlNode> _bottomNodes = new();
    
    public DocumentParser(string rawDocText)
    {
        rawDocText = rawDocText.Replace("<div></div>", ""); 
        doc = new HtmlDocument();
        doc.LoadHtml(rawDocText);
        CleanUp(doc.DocumentNode);
    }

    private void CleanUp(HtmlNode rootNode)
    {
        var toRemove = new List<HtmlNode>();
        foreach (var descendant in rootNode.Descendants())
        {
            bool shouldRemove = false;
            
            if (descendant == null)
            {
                continue;
            }
            
            if (descendant.InnerHtml.Trim() == "")
            {
                shouldRemove = true;
            }
            
            if (_tagsToRemove.Contains(descendant.OriginalName.ToLower()))
            {
                shouldRemove = true;
            }

            if (descendant.OriginalName == "#text")
            {
                if (descendant.InnerText.Trim() == "")
                {
                    shouldRemove = true;
                }
            }
            
            if (shouldRemove)
            {
                toRemove.Add(descendant);
                continue;
            }
            
            if (descendant.Attributes.Contains("href"))
            {
                if (Regex.IsMatch(descendant.Attributes["href"].Value, @"\/marketplace\/item\/\d*\/.*?"))
                {
                    descendant.Attributes["href"].Value = Regex.Replace(descendant.Attributes["href"].Value, @"(?<actualURL>\/marketplace\/item\/\d*\/).*",
                        @"https://www.facebook.com${actualURL}");
                }
            }
            
            descendant.RemoveClass();   // removes all classes from the node
            var attrsToRemove = 
                descendant.Attributes.Where(attribute => _attributesToRemove
                    .Contains(attribute.Name.ToLower()))
                    .ToList();
                
            foreach (var attribute in attrsToRemove)
            {
                descendant.Attributes.Remove(attribute);
            }
            
            if (descendant.OriginalName != "#text" && 
                descendant.ChildNodes.Count == 0 || 
                (descendant.ChildNodes.Count == 1 && descendant.ChildNodes[0].OriginalName == "#text")
                )
            {
                _bottomNodes.Add(descendant);
            }
        }
        
        foreach (var node in toRemove)
        {
            try
            {
                node.Remove();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to remove node for reason: {e.Message}");
            }
        }

        RemoveSoloParents(rootNode);
    }
    
    public void WriteToFile(string path)
    {
        doc.Save(path);

        var docText = File.ReadAllText(path);

        XDocument xmlDoc = XDocument.Parse(docText.Replace("&nbsp;", ""));
        File.WriteAllText(path, xmlDoc.ToString());
    }

    public void RemoveSoloParents(HtmlNode rootNode)
    {
        docChanged = false;
        RemoveSoloWrapperParents(rootNode);

        if (docChanged)
        {
            RemoveSoloParents(rootNode);
        }
        
    }
    
    /**
     *   BEFORE:
     *   <parent>
     *      ...
     *      <node>
     *          <child></child>
     *      </node>
     *      ...
     *  </parent>
     *
     *   AFTER:
     *   <parent>
     *      ...
     *      <child></child>
     *      ...
     *   </parent>
     */
    public void RemoveSoloWrapperParents(HtmlNode node)
    {
        var children = node.ChildNodes;
        var parentNode = node.ParentNode;
        

        if (node.Attributes.Contains("href"))
        {
            return;
        }
        
        if (children.Count == 0 && parentNode != null)
        {
            return;
        }
        
        if (children.Count == 1 && parentNode != null)
        {
            var child = children[0];
            
            if (child.OriginalName == "#text" && child.InnerHtml.Trim() != "")
            {
                return;
            }

            docChanged = true;
            // Console.WriteLine("TRANSFORMING");
            // Console.WriteLine($"<{parentNode.OriginalName,-2}>                 |  " + "");
            // Console.WriteLine($"    <{node.OriginalName,-2}>             |  "       + $"<{parentNode.OriginalName,-2}>");
            // Console.WriteLine($"        <{child.OriginalName,-2}/>        |  "      + $"    <{child.OriginalName,-2}/>");
            // Console.WriteLine($"    <{node.OriginalName,-2}/>            |  "       + $"</{parentNode.OriginalName,-2}>");
            // Console.WriteLine($"</{parentNode.OriginalName,-2}>                |  " + "");
            
            parentNode.ReplaceChild(child, node);
            var replacementNode = parentNode.ChildNodes[parentNode.ChildNodes[child]];
            
            foreach(var grandChild in replacementNode.ChildNodes.ToList())
            {
                if (grandChild == null)
                {
                    continue;
                }
                try
                {
                    RemoveSoloWrapperParents(grandChild);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{replacementNode.OriginalName}:{grandChild.OriginalName}     {e.Message}");
                }
            }
        }
        
        else
        {
            foreach(var child in children.ToList())
            {
                foreach (var grandChild in child.ChildNodes.ToList())
                { 
                    RemoveSoloWrapperParents(grandChild);
                }
            }
        }
        
    }


    public List<HtmlNode> GetCarNodes()
    {
        var carNodes = new List<HtmlNode>();
        foreach (var descendant in doc.DocumentNode.Descendants())
        {
            if (descendant.OriginalName == "a" && descendant.Attributes.Contains("href"))
            {
                if (descendant.Attributes["href"].Value.StartsWith("https://www.facebook.com/marketplace/item/"))
                {
                    // CleanUp(descendant);
                    descendant.InnerHtml =
                        descendant.InnerHtml.Replace("<div><div><div><div><div><div></div></div></div></div></div>",
                            "");
                    carNodes.Add(descendant);
                }
            }
        }
        
        return carNodes;
    }
    
}