using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{
    /// <summary>
    /// Used as a structure which holds information about each World'engine collection of ui_elements
    /// This is added to a World class in order to become useful: public tile_map[,] world_map; 
    /// </summary>
    public struct tile_map
    {
        public short tile_id;               // 0 - 65535 possible Tile definitons
        public byte tile_variant;           // 0 - 255   possible variant definitions, currently  (version 0.2) using 6 base sprites with 16 variants
        public Rectangle tile_rec;
        public Rectangle[] corners;
        public int water_units;             // based on tile_size - water volume should go from 1 to 100 % and 5% = 1 px
        public int source_x, source_y;
        public int pressure;                // record pressure value in this get_cell_address, if this get_cell_address's pressure - above get_cell_address's pressure > 1, move water up
        public bool flow;                   // only assigned when tile received water from above
        const int down_rate = 16;

        /// <summary>
        /// Tile map constructor
        /// </summary>
        /// <param name="id">tile id</param>
        /// <param name="variant">tile variant - based on neighbors</param>
        /// <param name="vol">water volume</param>
        public tile_map(short id, byte variant, int vol = 0)
        {
            tile_id = id;
            tile_variant = variant;
            tile_rec = new Rectangle(20, 0, 20, 20); // default rectangle is get_cell_address #2 - base Tile
            corners = new Rectangle[4];
            corners[0] = new Rectangle(60, 0, 20, 20); // get_cell_address #4 - empty
            corners[1] = new Rectangle(60, 0, 20, 20); // get_cell_address #4
            corners[2] = new Rectangle(60, 0, 20, 20); // get_cell_address #4
            corners[3] = new Rectangle(60, 0, 20, 20); // get_cell_address #4
            water_units = vol;
            flow = false;

            source_x = -1;
            source_y = -1;
            pressure = 0; // initial pressure is 0
        }

        /// <summary>
        /// Water flowing algorithm.Called on destination get_cell_address.
        /// </summary>
        /// <param name="source">Tile from which water is transferred </param>
        /// <param name="w"> direction of the flow </param>
        public void transfer_water_from(ref tile_map source)
        {
            int rate = down_rate;
            int capacity = this.until_full();

            if (capacity >= rate && source.water_units >= rate) // source has more than 10, destination needs more than 10
            {
                this.water_units += rate;
                source.water_units -= rate;
            }
            else if (capacity < rate && source.water_units >= capacity)  // source has more than 10 but destination can't hold that many
            {
                source.water_units -= capacity;
                this.water_units = 100;
            }
            else if ((capacity < rate && source.water_units < capacity) // source has less than destination can hold and destination can hold less than 10
                || (capacity >= rate && source.water_units < rate))   // source has less than 10 and destination can hold all of it
            {
                this.water_units += source.water_units;
                source.water_units = 0;
            }
            // update air ui_elements that have become water
            if (this.water_units > 0 && this.tile_id == 0)
            {
                this.tile_id = -1;
            }
            else if (source.water_units == 0 && source.tile_id == -1)
            {
                source.tile_id = 0;
                source.pressure = 0;
            }
        }
      
        /// <summary>
        /// determine if the Tile id is 0
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_air()
        {
            if (tile_id == 0)
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// determine if the tile is full of water
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_full()
        {
            return water_units >= 100 ? true : false;
        }
        /// <summary>
        /// determine how much water is needed for filling up the tile
        /// </summary>
        /// <returns>number of water units until full</returns>
        public int until_full()
        {
            return 100 - water_units;
        }
        /// <summary>
        /// determine how much water is needed for emptying up the tile
        /// </summary>
        /// <returns>number of water units until empty</returns>
        public int until_empty()
        {
            return water_units;
        }
    }
}