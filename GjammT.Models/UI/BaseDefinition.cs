using GjammT.Core.Interfaces;

namespace GjammT.Models.UI;

public class BaseDefinition(string resourceName) : IGjammT
{
    public string ResourceName = resourceName;
}