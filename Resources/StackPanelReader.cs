using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Game.Internals;

namespace AutoFollow.Resources
{
    public static class StackPanelReader
    {
        public class StackPanelItem
        {
            public UIElement ItemElement { get; set; }
            public UIElement TextElement { get; set; }
        }

        /// <summary>
        /// Gets the Item UIElements and Text UIElement from inside a StackPanel
        /// </summary>
        public static List<StackPanelItem> GetStackPanelItems(this UIElement stackPanel)
        {
            Func<UIElement, UIElement> findTextNode = null;
            findTextNode = elements =>
            {
                var children = UIElement.GetChildren(elements).ToList();

                if (!children.Any())
                    return null;

  
                foreach (var el in children)
                {
                    var lowerName = el.Name.ToLowerInvariant();
                    if (lowerName.EndsWith(".text") || lowerName.EndsWith(".name"))
                        return el;
                }

                foreach (var el in children)
                {
                    if (el.HasText)
                        return el;
                }

                foreach (var el in children)
                {
                    return findTextNode(el);
                }

                return null;
            };

            var items = UIElement.GetChildren(stackPanel).ToList();
            var result = new List<StackPanelItem>();

            if (!items.Any() || !stackPanel.Name.ToLowerInvariant().Contains("stackpanel"))
                return result;

            foreach (var item in items)
            {
                var text = findTextNode(item);
                result.Add(new StackPanelItem
                {
                    ItemElement = item,
                    TextElement = text ?? item
                });
            }

            return result;
        }

        public static int GetStackPanelHashCode(this UIElement stackPanel)
        {
            return GetStackPanelItems(stackPanel).Aggregate(0, (current, node) => current ^ node.GetHashCode());
        }

        public static int GetStackPanelHashCode(List<StackPanelItem> items)
        {
            return items.Aggregate(0, (current, node) => current ^ node.TextElement.Text.GetHashCode());
        }

    }
}
