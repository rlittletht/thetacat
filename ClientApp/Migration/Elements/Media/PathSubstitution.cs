using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.String;

namespace Thetacat.Migration.Elements.Metadata.UI.Media;

class PathSubstitution
{
    public string From { get; set; } = Empty;
    public string To { get; set; } = Empty;
}