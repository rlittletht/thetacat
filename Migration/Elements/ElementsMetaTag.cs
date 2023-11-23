using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

public class ElementsMetaTag
{
    public string? Name { get; set; }
    public string? ID { get; set; }
    public string? ParentID { get; set; }
    public string? ParentName { get; set; }
    public string? ElementsTypeName { get; set; }
}