using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EditorEngine
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
        /// <summary>
        /// Vertical sector - will be contained in the horizontal sectors
        /// </summary>
        /// <param name="xs">start of the sector</param>
        /// <param name="xe">end of the sector</param>
        /// <param name="type">sector content, e.g. label, slider etc.</param>
        public vertical_sector(int xs, int xe, sector_content type)
        {
            this.x_start = xs;
            this.x_end = xe;
            this.type = type;
        }
        /// <summary>
        /// Get sector content type
        /// </summary>
        /// <returns>sector_content enum value</returns>
        public sector_content get_content_type()
        {
            return type;
        }
        /// <summary>
        /// Get starting position
        /// </summary>
        /// <returns>position in pixels</returns>
        public int get_xs()
        {
            return x_start;
        }
        /// <summary>
        /// Get end position
        /// </summary>
        /// <returns>position in pixels</returns>
        public int get_xe()
        {
            return x_end;
        }
        /// <summary>
        /// Get sector width
        /// </summary>
        /// <returns>width in pixels</returns>
        public int get_width()
        {
            return (x_end - x_start);
        }
    }
    /// <summary>
    /// Horizontal sector - contains vertical sectors
    /// </summary>
    [Serializable()]
    public struct horizontal_sector
    {
        private int y_start;                      // sector top border
        private int y_end;                        // sector bottom border
        private List<vertical_sector> verticals;  // list of vertical sector
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ys">start</param>
        /// <param name="ye">end</param>
        public horizontal_sector(int ys, int ye)
        {
            this.y_start = ys;
            this.y_end = ye;
            verticals = new List<vertical_sector>();
        }
        /// <summary>
        /// Add a vertical sector to this horizontal sector
        /// </summary>
        /// <param name="v">vertical sector struct</param>
        public void add_vertical(vertical_sector v)
        {
            verticals.Add(v);
        }
        /// <summary>
        /// Get a list of all vertical sectors
        /// </summary>
        /// <returns>List<vertical_sector>  type</returns>
        public List<vertical_sector> get_verticals()
        {
            return verticals;
        }
        /// <summary>
        /// Get starting position
        /// </summary>
        /// <returns>position in pixels</returns>
        public int get_ys()
        {
            return y_start;
        }
        /// <summary>
        /// Get end position
        /// </summary>
        /// <returns>position in pixels</returns>
        public int get_ye()
        {
            return y_end;
        }
        /// <summary>
        /// Get sector height
        /// </summary>
        /// <returns>height in pixels</returns>
        public int get_height()
        {
            return (y_end - y_start);
        }
    }
}
