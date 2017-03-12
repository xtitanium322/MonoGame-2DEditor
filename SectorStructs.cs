using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace beta_windows
{
    /// <summary>
    /// Zones used to create separations inside GUI stored_elements for display and functional purposes
    /// </summary>
    [Serializable()]
    public struct vertical_sector // compoonent for horizontal sector
    {
        private int x_start;         // sector left border - start
        private int x_end;           // sector right border - end
        private sector_content type; // what is displayed in the sector

        public vertical_sector(int xs, int xe, sector_content type)
        {
            this.x_start = xs;
            this.x_end = xe;
            this.type = type;
        }

        public sector_content get_content_type()
        {
            return type;
        }

        public int get_xs()
        {
            return x_start;
        }
        public int get_xe()
        {
            return x_end;
        }
        public int get_width()
        {
            return (x_end - x_start);
        }
    }
    /* horizontal sectors contain vertical sectors */
    [Serializable()]
    public struct horizontal_sector
    {
        private int y_start;                      // sector top border
        private int y_end;                        // sector bottom border
        private List<vertical_sector> verticals;  // list of vertical sector

        public horizontal_sector(int ys, int ye)
        {
            this.y_start = ys;
            this.y_end = ye;
            verticals = new List<vertical_sector>();
        }
        public void add_vertical(vertical_sector v)
        {
            verticals.Add(v);
        }
        public List<vertical_sector> get_verticals()
        {
            return verticals;
        }

        public int get_ys()
        {
            return y_start;
        }
        public int get_ye()
        {
            return y_end;
        }
        public int get_height()
        {
            return (y_end - y_start);
        }
    }
}
