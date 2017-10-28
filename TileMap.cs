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
/*
 * Used as a structure which holds information about each World'engine collection of ui_elements
 * This is added to a World class in order to become useful: public tile_map[,] world_map; 
 */
namespace EditorEngine
{
    public struct tile_map
    {
        // Tile_id 0 = air Tile, no sprite drawn, no collision detection, tile id -1 = water
        // variant 0 = base Tile (no connections)
        public short tile_id;          // 0 - 65535 possible Tile definitons
        public byte tile_variant;      // 0 - 255   possible variant definitions, currently  (version 0.2) using 6 base sprites with 16 variants
        public Rectangle tile_rec;
        public Rectangle[] corners;
        public int water_units;        // based on tile_size - water volume should go from 1 to 100 % and 5% = 1 px

        public int source_x, source_y;
        public int pressure; // record pressure value in this cell, if this cell's pressure - above cell's pressure > 1, move water up
        public bool flow;    // only assigned when tile received water from above
        // flow rates
        const int down_rate = 16;

        public tile_map(short id, byte variant, int vol = 0)
        {
            tile_id = id;
            tile_variant = variant;
            tile_rec = new Rectangle(20, 0, 20, 20); // default rectangle is cell #2 - base Tile
            corners = new Rectangle[4];
            corners[0] = new Rectangle(60, 0, 20, 20); // cell #4 - empty
            corners[1] = new Rectangle(60, 0, 20, 20); // cell #4
            corners[2] = new Rectangle(60, 0, 20, 20); // cell #4
            corners[3] = new Rectangle(60, 0, 20, 20); // cell #4
            water_units = vol;
            flow = false;

            source_x = -1;
            source_y = -1;
            pressure = 0; // initial pressure is 0
        }
        /// <summary>
        /// Water flowing algorithm.Called on destination cell.
        /// </summary>
        /// <param name="source">Tile from which water is transferred </param>
        /// <param name="w"> direction of the flow </param>
        /// 
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

        public void transfer_water_pressure(ref tile_map source)
        {
            this.water_units += 1;
            source.water_units -= 1;
        }

        // called on source cell
        /*public void transfer_water_horizontally(bool left, bool right, ref tile_map left_cell, ref tile_map right_cell)
        {
            int rate = side_rate; // 5

            if(right && left) // both cells exist
            {
                if(left_cell.water_units + rate <= this.water_units - 2*rate 
                 &&right_cell.water_units + rate <= this.water_units - 2*rate) // if both cells can receive water and won't overflow remaining source
                {
                    // do transfers
                    left_cell.water_units += rate;
                    right_cell.water_units += rate;
                    this.water_units -= 2 * rate;
                }
                else
                {
                    int temp_rate = (int)rate;
                    while (temp_rate > 1) // half the rate until it's 1
                    {
                        temp_rate /= 2; // divide in half

                        if (left_cell.water_units + temp_rate <= this.water_units - 2 * temp_rate
                           && right_cell.water_units + temp_rate <= this.water_units - 2 * temp_rate)
                        {
                            // do transfers
                            left_cell.water_units += temp_rate;
                            right_cell.water_units += temp_rate;
                            this.water_units -= 2 * temp_rate;
                            break;
                        }
                    }
                    // try 0.5 rate if above fails
                    /*if (left_cell.water_units + 0.5f    <= this.water_units - 1
                       && right_cell.water_units + 0.5f <= this.water_units - 1)
                    {
                        // do transfers
                        left_cell.water_units += 0.5f;
                        right_cell.water_units += 0.5f;
                        this.water_units -= 1;
                    }
                    // else skip without transfers
                }
            }
            
            if(left)
            {
                if(left_cell.water_units + rate <= this.water_units - rate)
                {
                    // do transfers
                    left_cell.water_units += rate;
                    this.water_units -= rate;
                }
                else
                {
                    int temp_rate = (int)rate;
                    while (temp_rate > 1) // half the rate until it's 1
                    {
                        temp_rate /= 2; // divide in half

                        if (left_cell.water_units + temp_rate <= this.water_units - temp_rate)
                        {
                            left_cell.water_units += temp_rate;
                            this.water_units -= temp_rate;
                            break;
                        }
                    }
                    // try 0.5 rate if above fails
                    /*if (left_cell.water_units + 0.5f <= this.water_units - 0.5f)
                    {
                        // do transfers
                        left_cell.water_units += 0.5f;
                        this.water_units -= 0.5f;
                    }
                    // else skip without transfers
                }
            }

            if(right)
            {
                if (right_cell.water_units + rate <= this.water_units - rate)
                {
                    // do transfers
                    right_cell.water_units += rate;
                    this.water_units -= rate;
                }
                else
                {
                    int temp_rate = (int)rate;
                    while (temp_rate > 1) // half the rate until it's 1
                    {
                        temp_rate /= 2; // divide in half

                        if (right_cell.water_units + temp_rate <= this.water_units - temp_rate)
                        {
                            right_cell.water_units += temp_rate;
                            this.water_units -= temp_rate;
                            break;
                        }
                    }
                    // try 0.5 rate if above fails
                    /*if (right_cell.water_units + 0.5f <= this.water_units - 0.5f)
                    {
                        // do transfers
                        right_cell.water_units += 0.5f;
                        this.water_units -= 0.5f;
                    }
                    // else skip without transfers
                }
            }

        // update air ui_elements that have become water
            if (this.water_units > 0 && this.tile_id == 0)
            {
                this.tile_id = -1;
            }

            if (left_cell.water_units == 0 && left_cell.tile_id == -1)
            {
                left_cell.tile_id = 0;
                left_cell.pressure = 0;
            }
            if (left_cell.water_units > 0)
                left_cell.tile_id = -1;

            if (right_cell.water_units == 0 && right_cell.tile_id == -1)
            {
                right_cell.tile_id = 0;
                right_cell.pressure = 0;
            }
            if (right_cell.water_units > 0)
                right_cell.tile_id = -1;
        }*/
        // determine if the Tile id is 0
        public bool is_air()
        {
            if (tile_id == 0)
            {
                return true;
            }

            return false;
        }
        public bool is_full()
        {
            return water_units >= 100 ? true : false;
        }
        public bool is_dry()
        {
            return water_units <= 0 ? true : false;
        }
        public int until_full()
        {
            return 100 - water_units;
        }
        public int until_empty()
        {
            return water_units;
        }

        public bool contains_between_0and100()
        {
            if (water_units > 0 && water_units < 100)
                return true;

            return false;
        }
    }
}