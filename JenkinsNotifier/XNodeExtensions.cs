using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace JenkinsNotifier
{
  internal static class XNodeExtensions
  {
    public static T GetValue<T>(this XNode parentNode, string expression)
    {
      object node = parentNode.XPathEvaluate(expression);
      if (node == null)
        throw new ApplicationException(string.Format("Unable to locate node '{0}'!", (object) expression));
      string nodeValue = XNodeExtensions.GetNodeValue(node);
      if (nodeValue == null)
        throw new ApplicationException("Unable to get node value!");
      return nodeValue.To<T>();
    }

    public static T TryGetValue<T>(this XNode parentNode, string expression, T defaultValue = default)
    {
      string nodeValue = XNodeExtensions.GetNodeValue(parentNode.XPathEvaluate(expression));
      return string.IsNullOrEmpty(nodeValue) ? defaultValue : nodeValue.To<T>();
    }

    public static T Wrap<T>(this XNode parentNode, string expression, Func<XElement, T> wrapFunc)
    {
      XElement xelement = parentNode.XPathSelectElement(expression);
      return xelement == null ? default (T) : wrapFunc(xelement);
    }

    public static IEnumerable<T> WrapGroup<T>(
      this XNode parentNode,
      string expression,
      Func<XElement, T> wrapFunc)
    {
      return parentNode.XPathSelectElements(expression).Select<XElement, T>(wrapFunc);
    }

    private static string GetNodeValue(object node)
    {
      if (node is IEnumerable)
        node = ((IEnumerable) node).Cast<object>().FirstOrDefault<object>();
      switch (node)
      {
        case null:
          return (string) null;
        case XElement xelement:
          return xelement.Value;
        case XAttribute xattribute:
          return xattribute.Value;
        default:
          throw new ApplicationException(string.Format("Unable to retrieve value from node of type '{0}'!", (object) node.GetType().Name));
      }
    }
  }
}
