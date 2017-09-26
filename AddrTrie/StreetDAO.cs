using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddrTrie
{
    public class StreetDAO
    {
        public IEnumerable<string> GetAllDistinctStreets()
        {
            using (var fp = new FeaturePenetrationsEntities())
            {
                foreach (var s in fp.StreetIndexes.Select(s => s.Name.ToUpper()))
                    yield return s;
            }
        }
    }

}
