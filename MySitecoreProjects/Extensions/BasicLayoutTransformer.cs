using Sitecore.Data;
using Sitecore.LayoutService.ItemRendering;
using Sitecore.LayoutService.Placeholders;
using Sitecore.LayoutService.Serialization;
using System;
using System.Linq;

namespace MySitecoreProjects.LayoutServices.Extensions
{
    public class BasicLayoutTransformer : ILayoutTransformer
    {
        protected readonly IPlaceholderTransformer PlaceholderTransformer;

        public BasicLayoutTransformer(IPlaceholderTransformer placeholderTransformer) => this.PlaceholderTransformer = placeholderTransformer;

        public object Transform(RenderedItem rendered) => (object)new
        {
            name = rendered.Name,
            components = rendered.Elements.Select<RenderedPlaceholderElement, object>(new Func<RenderedPlaceholderElement, object>(this.TransformElement))
        };

        public object Transform(RenderedPlaceholder rendered) => (object)new
        {
            components = rendered.Elements.Select<RenderedPlaceholderElement, object>(new Func<RenderedPlaceholderElement, object>(this.TransformElement))
        };

        public object Transform(RenderedPlaceholderElement rendered) => this.PlaceholderTransformer.TransformPlaceholderElement(rendered);

        protected object TransformElement(RenderedPlaceholderElement element)
        {
            var styleRenderParam = "";
            var variantRenderParam = "";
            var gridsettingRenderParam = "";
            bool first = true;
            var sitecoreContext = Sitecore.Context.Database;
            foreach (var elementJSON in ((Sitecore.LayoutService.ItemRendering.RenderedJsonRendering)element).RenderingParams) 
            {
                if (elementJSON.Key.ToUpper() == "STYLES" || elementJSON.Key.ToUpper() == "GRIDPARAMETERS")
                {
                    string fieldValue = elementJSON.Key.ToUpper() == "GRIDPARAMETERS" ? "Class" : "Value";
                    first = true;
                    var param = "";
                    if(elementJSON.Value!= null)
                    {
                        foreach(var styleID in elementJSON.Value.Split('|')) 
                        {
                            Sitecore.Data.Items.Item DBitem = sitecoreContext.GetItem(new ID(styleID));
                            if (first)
                            {
                                param = DBitem.Fields[fieldValue].Value;
                                first = false;
                            }
                            else { param = param + "," + DBitem.Fields[fieldValue].Value; }
                        }
                    }
                    if (elementJSON.Key.ToUpper() == "STYLES")
                    {
                        styleRenderParam = param;
                    }
                    else 
                    {
                        gridsettingRenderParam = param;
                    }
                }
                if (elementJSON.Key.ToUpper() == "FIELDNAMES") 
                {
                    Sitecore.Data.Items.Item DBitem = sitecoreContext.GetItem(new ID(elementJSON.Value));
                    variantRenderParam = DBitem.Name;
                }
            }
            if (element is RenderedJsonRendering)
            {
                RenderedJsonRendering renderedJsonRendering = (RenderedJsonRendering)element;
                if (renderedJsonRendering.RenderingParams.ContainsKey("RenderingIdentifier"))
                    return (object)new
                    {
                        name = renderedJsonRendering.ComponentName,
                        id = renderedJsonRendering.RenderingParams["RenderingIdentifier"],
                        path = element.Placeholder.Path,
                        gridsetting = gridsettingRenderParam,
                        styles = styleRenderParam,
                        variant = variantRenderParam,
                        contents = (element.Contents ?? (object)string.Empty)
                    };
            }
            return (object)new
            {
                name = (element is RenderedJsonRendering ? ((RenderedJsonRendering)element).ComponentName : element.Name),
                path = element.Placeholder.Path,
                gridsetting = gridsettingRenderParam,
                styles = styleRenderParam,
                variant = variantRenderParam,
                contents = (element.Contents ?? (object)string.Empty)
            };
        }
    }
}
