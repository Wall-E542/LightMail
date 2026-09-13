using Ganss.Xss;

public class HtmlMailSanitizer
{
    private readonly HtmlSanitizer _sanitizer;


    public HtmlMailSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Tags
        string[] tags =
        {
            "table", "tbody", "thead", "tr", "td", "th",
            "div", "span",
            "p", "br", "hr",
            "a",
            "img",
            "strong", "b", "i", "u"
        };

        foreach(var tag in tags)
            _sanitizer.AllowedTags.Add(tag);


        // Attribute
        string[] attributes =
        {
            "style",
            "class",
            "href",
            "src",
            "alt",
            "width",
            "height",
            "align",
            "title"
        };

        foreach(var attr in attributes)
            _sanitizer.AllowedAttributes.Add(attr);


        // sichere CSS Eigenschaften
        string[] css =
        {
            "color",
            "background-color",
            "font-size",
            "font-family",
            "text-align",
            "margin",
            "padding"
        };

        foreach(var property in css)
            _sanitizer.AllowedCssProperties.Add(property);
    }


    public string Sanitize(string html)
    {
        return _sanitizer.Sanitize(html);
    }
}