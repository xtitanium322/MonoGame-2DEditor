﻿using System;
using System.Diagnostics;
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
 * Generates a playable map for the hero to explore
 */
namespace beta_windows
{
    public class World //map generator
    {
        private int w;                             // length/width
        private int h;                             // height 
        private const int tile_size = 20;          // size of square in pixels
        private String world_name;                 // name of this playable world
        private bool edit_mode;                    // is the world in edit mode?
        private Vector2 map_origin;                // map origin (top left corner)
        public tile_map[,] world_map;             // contains all Tile definitions for this world
        private Stack<Vector2> updated_cells;      // contains every cell that has been updated during current frame, cells deleted after calculations
        private Color[] sky_color;                 // an array of values for world background color in relation to world time
        private Color sky;                         // current color of the sky
        //private Color grid_color;                  // used to draw editor grid
        public List<PointLight> world_lights;      // a list of all highlighted lights   
        //public Editor editor;                      // World editor class - for adding/deleting/selecting/tweaking ui_elements,lights etc.
        //public float grid_transparency_value;      // used to display cell placement grid
        //public int gridcolor_r;
        //public int gridcolor_g;
        //public int gridcolor_b;
        Rectangle[] corner_src;
        // constructors
        public World(Engine engine, ContentManager content, String name, int length, int height)
        {
            world_map = new tile_map[length, height]; // create an array which accepts tile_map structures
            edit_mode = false;
            w = length;
            h = height;
            world_name = name;
            //edit_tile_number = 1;
            map_origin = new Vector2(0, 0);
            //grid_transparency_value = 0.25f;

            //world_init();
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    world_map[i, j] = new tile_map(0, 0); // create air ui_elements, id=0, variant = 0
                }
            }
            //
            // create memory for updated cells
            updated_cells = new Stack<Vector2>();
            // create World Light list
            world_lights = new List<PointLight>();
            // create world editor object
            //editor = new Editor(engine);


            sky = new Color(135, 206, 235);          // default sky color
            // sky colors (effects to be added for sun and moon transitions
            sky_color = new Color[3];
            sky_color[0] = new Color(10, 10, 10);      // midnight color 
            sky_color[1] = new Color(135, 206, 235); // noon color
            sky_color[2] = new Color(0, 191, 250);     // day color 
            //grid color assignment
            //gridcolor_r = 255;
            //gridcolor_g = 255;
            //gridcolor_b = 255;
            //grid_color = new Color(gridcolor_r, gridcolor_g, gridcolor_b);

            corner_src = new Rectangle[4];
        }

        public void LoadContent(ContentManager content, Engine engine)
        {
            //engine.get_editor().LoadContent(content, engine, this);
        }

        public void draw_map(Engine engine, SpriteBatch sb)
        {
            // limit amount of time it takes for map rendering
            // based on the world size: 600x120 = 72000 ui_elements = 72000 loop iterations and a handful of draw calls = about 8 ms time. Overhead caused by the need to iterate entire array
            // update #1: limit number of loop iterations based on visible tile indexes - final result = max 6ms for rendering full screen of ui_elements
            // update #2: remove all "new" keywords from loop - insignificant improvement
            // update #3: draw same texture - reduce texture switching operations to overall number of textures
            Vector2 min_limit = min_visible_tile(engine);
            Vector2 max_limit = max_visible_tile(engine);
            Rectangle src = new Rectangle();
            Vector2 position = Vector2.Zero;

            // tile loop - go through all ui_elements
            foreach (tile_struct t in Tile.tile_list)
            {
                Texture2D current_tile = t.tile_texture; // assign current texture

                for (int i = (int)min_limit.X - 1; i < (int)max_limit.X; i++)
                {
                    //for (int j = 0; j < height; j++)
                    for (int j = (int)min_limit.Y - 1; j < (int)max_limit.Y; j++)
                    {
                        // skip all elements that are not this texture
                        if (world_map[i, j].tile_id != t.id)
                            continue;
                        // define a crop rectangle - which variant of the Tile will be used
                        src = world_map[i, j].tile_rec;
                        corner_src[0] = world_map[i, j].corners[0]; // assign rectangles for corners - without creating new Rectangle every frame
                        corner_src[1] = world_map[i, j].corners[1];
                        corner_src[2] = world_map[i, j].corners[2];
                        corner_src[3] = world_map[i, j].corners[3];

                        if (world_map[i, j].is_air() == false)
                        {// draw tile sprite
                            position.X = i * tile_size;
                            position.Y = map_origin.Y + j * tile_size;

                            engine.xna_draw(
                            Tile.find_tile(world_map[i, j].tile_id), // find a texture specified by Tile_id in tile_map -> Tile_id_Listing -> Tile_id
                            position - engine.get_camera_offset(), // where
                            src, Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.5f);
                            // draw corners
                            for (int k = 0; k < 4; k++)
                            {
                                if (corner_src[k].X != 60) // 60 represents 4th cell - empty_texture corner
                                {
                                    engine.xna_draw(
                                    Tile.find_tile(world_map[i, j].tile_id), // find a texture specified by Tile_id in tile_map -> Tile_id_Listing -> Tile_id
                                    position - engine.get_camera_offset(), // where
                                    corner_src[k], Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.6f);
                                }
                            }
                        } // end draw ui_elements
                    }// end height loop
                }// end width loop  
            }// end foreach loop

            // water tile loop
            for (int i = (int)min_limit.X - 1; i < (int)max_limit.X; i++)
            {
                for (int j = (int)min_limit.Y - 1; j < (int)max_limit.Y; j++)
                {
                    if (world_map[i, j].tile_id != -1) // only draw water ui_elements
                        continue;

                    tile_map current;
                    tile_map above_current;
                    tile_map below_current;
                    tile_map right;
                    tile_map left;

                    float water_vol = world_map[i, j].until_empty(); // amount of air
                    int air_pixels = tile_size - (int)((water_vol / 100f) * tile_size);

                    src.X = 0; src.Y = air_pixels; src.Width = tile_size; src.Height = tile_size - air_pixels;
                    position.X = i * tile_size;
                    position.Y = map_origin.Y + j * tile_size + air_pixels;
                    // draw this cell
                    if (valid_array(i, j - 1) && valid_array(i, j + 1) && valid_array(i, j) && valid_array(i - 1, j - 1) && valid_array(i + 1, j - 1))
                    {
                        current = world_map[i, j];
                        above_current = world_map[i, j - 1];
                        below_current = world_map[i, j + 1];
                        right = world_map[i + 1, j];
                        left = world_map[i - 1, j];

                        if (current.water_units == 100)
                        {
                            src.Y = 0; src.Height = tile_size;
                            position.Y = map_origin.Y + j * tile_size;

                            engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                             engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                        else // not full
                        {
                            if (current.water_units > 0 && above_current.tile_id == 0) // not full and nothing above
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            else if ((current.water_units > 0 && above_current.water_units > 0 && above_current.tile_id == -1) || current.flow == true) // not full / something above
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);

                                src.Y = 0; src.Height = air_pixels;
                                position.Y = map_origin.Y + j * tile_size;
                                // flow
                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                  engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.25f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            else
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                        }
                    }
                }
            }
            // reset after special effects have been drawn
            //reset_water();
        }
        ///===============WATER testing=========================================================================================================
        // Update world: Tile connections
        public void update_world(WorldClock c, Engine engine)
        {
            // update GUI option connected values
            /*grid_transparency_value = engine.get_editor().GUI.get_slider_value(actions.update_slider_grid_transparency);
            gridcolor_r = (int)engine.get_editor().GUI.get_slider_value(actions.update_slider_grid_color_red);
            gridcolor_g = (int)engine.get_editor().GUI.get_slider_value(actions.update_slider_grid_color_green);
            gridcolor_b = (int)engine.get_editor().GUI.get_slider_value(actions.update_slider_grid_color_blue);
            grid_color = new Color(gridcolor_r, gridcolor_g, gridcolor_b);*/
            // calculate sky color
            sky = calculate_sky_color(c);
            // new code: calculates changes only for updated cells
            foreach (Vector2 cell in updated_cells)
            {
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        if (tile_exists(cell + new Vector2(i, j)))
                            tile_connections(cell + new Vector2(i, j));    // Update 9 cell region
                    }
                }
            }
            // clear update cell matrix
            if (updated_cells.Count > 0)
                updated_cells.Clear(); // delete cells from stack

            // editor updates
            //editor.Update(this, engine);
            // water updates    

            water_test(engine);
        }

        // limit to area 100v100
        public void water_test(Engine engine)
        {
            int rate = 7; // vertical rate

            for (int y = h; y > 0; y--) // bottom --> top
            //for (int y = 0; y < h; y++)
            {
                //for(int x = 100; x > 0; x--) // better visuals, buggy calculation
                for (int x = 0; x < 100; x++)  // worse visuals, better calculation (limited to a 100 cell zone in width
                {
                    // --------------------------------------------
                    // VERTICAL flow
                    // --------------------------------------------
                    if (valid_array(x, y + 1) && world_map[x, y + 1].pressure - world_map[x, y].pressure <= 1)
                    {
                        // calculations and transfers
                        if (world_map[x, y].tile_id == -1 && world_map[x, y + 1].tile_id <= 0 && world_map[x, y + 1].water_units < 100) // current (water); target (air, water, not full)
                        {
                            if (world_map[x, y].water_units > rate && world_map[x, y + 1].water_units + rate <= 100) // standard
                            {
                                world_map[x, y + 1].water_units += rate;
                                world_map[x, y].water_units -= rate;

                                if (world_map[x, y + 1].tile_id == 0)
                                    world_map[x, y + 1].tile_id = -1;

                                world_map[x, y + 1].flow = true;
                                world_map[x, y].flow = true;
                            }
                            else // not standard
                            {
                                if (world_map[x, y + 1].water_units + world_map[x, y].water_units <= 100) // standard
                                {
                                    world_map[x, y + 1].water_units += world_map[x, y].water_units;
                                    world_map[x, y].water_units = 0;
                                    world_map[x, y].tile_id = 0;
                                    world_map[x, y].pressure = 0;

                                    if (world_map[x, y + 1].tile_id == 0)
                                        world_map[x, y + 1].tile_id = -1;

                                    world_map[x, y + 1].flow = true;
                                    world_map[x, y].flow = true;
                                }
                                else // not full transfer
                                {
                                    int remaining = 100 - world_map[x, y + 1].water_units;
                                    world_map[x, y + 1].water_units = 100;
                                    world_map[x, y].water_units -= remaining;

                                    if (world_map[x, y + 1].tile_id == 0)
                                        world_map[x, y + 1].tile_id = -1;

                                    world_map[x, y + 1].flow = true;
                                    world_map[x, y].flow = true;
                                }
                            }
                        }
                    }// vertical flow end

                    // --------------------------------------------
                    // HORIZONTAL flow
                    // --------------------------------------------

                    // new right transfer test
                    int preferred_rate = 8;
                    int actual_rate = 0; // calculated for each pair of cells
                    int sx, sy; // source coordinates
                    int tx, ty; // target coordinates

                    if (valid_array(x, y) && valid_array(x + 1, y)) // check initial current and right cells to be within borders
                    {
                        //      for RIGHT transfers - set of rules:
                        // 0. if right cell has the same or smaller number of water units and it's not 100 - break the loop without transfer
                        // 1. calculate actual rate - if within 1-preferred rate = ok, if more - set to preferred rate, if 0 - check rule 1A
                        // 1A. check cell above current source - if it's water make it new source but add 100 to water_units for calculation purposes. then recalculate actual rate and transfer
                        //      if rule 1A was used - break out of the for loop after transfer
                        // 2. if there is no cell above - continue offset loop until good target cell is found or termination condition met (solid cell)
                        //      note: if air cell is found - it counts as target with 0 units only if it has 100unit cell or solid below it.
                        for (int offset = 1; offset < w; offset++)
                        {
                            // variables setup
                            sx = x; sy = y;          // current source
                            tx = x + offset; ty = y; // current target
                            // check valid array condition
                            if (!valid_array(tx, ty))
                                break;
                            // check for solid cell termination condition
                            if (world_map[tx, ty].tile_id > 0)
                                break;
                            // check if source is air or solid
                            if (world_map[sx, sy].tile_id >= 0)
                                break;
                            // rule 0. source <= target
                            if (world_map[sx, sy].water_units <= world_map[tx, ty].water_units)
                            {
                                if (world_map[sx, sy].water_units < world_map[tx, ty].water_units)
                                    break;
                                else // additional rule allows for 100 water units in all cells before pressure calculations
                                {
                                    continue;
                                }
                            }
                            // rule 1.
                            actual_rate = calculate_transfer_rate(sx, sy, tx, ty);
                            if (actual_rate >= preferred_rate)
                            {
                                actual_rate = preferred_rate;
                            }
                            //--------------------------------------
                            // direct TRANSFER conditions
                            //--------------------------------------
                            if (actual_rate == 0 && world_map[sx, sy].water_units == 100)
                            {
                                if (valid_array(tx, ty + 1)
                                    && world_map[tx, ty + 1].tile_id == 0)
                                    break;
                                //-------------------------------------------------------------standard section
                                // enable rule 1A.
                                if (valid_array(sx, sy - 1) && world_map[sx, sy - 1].water_units > 0) // something above source
                                {
                                    sy = sy - 1; // assign new source coordinates
                                    actual_rate = calculate_transfer_rate(sx, sy, tx, ty, 100); // calculate new rate with adjustment 
                                    // adjust new rate: if more than preferred - set to preferred
                                    if (actual_rate > preferred_rate)
                                        actual_rate = preferred_rate;
                                    // complete transfer by rule 1A (this can leave new source cell empty, check at the end and assign air if needed)
                                    // target cell can overflow, if it does, create new water cell above and fill it with remaining water units
                                    if (world_map[tx, ty].water_units + actual_rate <= 100) // target doesn't overflow
                                    {
                                        if (world_map[sx, sy].water_units - actual_rate < 0) // if there is not enough in current source cell for transfer to be successful
                                        {
                                            int remainder = actual_rate - world_map[sx, sy].water_units;

                                            world_map[sx, sy].water_units = 0;
                                            world_map[sx, sy].tile_id = 0;
                                            world_map[sx, sy].pressure = 0;

                                            world_map[sx, sy + 1].water_units -= remainder;
                                            world_map[tx, ty].water_units += actual_rate;

                                            break;
                                        }
                                        else // enough in cell above original source
                                        {
                                            world_map[sx, sy].water_units -= actual_rate;
                                            world_map[tx, ty].water_units += actual_rate;
                                            // check new source cell: assign air if needed
                                            if (world_map[sx, sy].water_units == 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy].pressure = 0;
                                            }

                                            break;
                                        }
                                    }
                                    else // target overflows
                                    {
                                        if (valid_array(tx, ty - 1) && world_map[tx, ty - 1].tile_id <= 0) // make sure new cell can be created above current target
                                        {
                                            int remainder = actual_rate - (100 - world_map[tx, ty].water_units); // tx and ty have been confirmed valid earlier in the code
                                            // transfer + create new water cell
                                            world_map[sx, sy].water_units -= actual_rate;

                                            world_map[tx, ty].water_units = 100; // set initial target to full capacity
                                            world_map[tx, ty - 1].tile_id = -1;
                                            world_map[tx, ty - 1].water_units = remainder;
                                            // check new source cell: assign air if needed
                                            if (world_map[sx, sy].water_units == 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy].pressure = 0;
                                            }

                                            if (world_map[sx, sy].water_units < 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy + 1].water_units += world_map[sx, sy].water_units; // add negative units to cell below
                                                world_map[sx, sy].water_units = 0; // reset
                                                world_map[sx, sy].pressure = 0;
                                            }

                                            break;
                                        }
                                        else if (valid_array(tx, ty - 1) && world_map[tx, ty - 1].tile_id > 0) // do partial transfer
                                        {
                                            if (actual_rate + world_map[tx, ty].water_units > 100)
                                                actual_rate = 100 - world_map[tx, ty].water_units;

                                            world_map[tx, ty].water_units += actual_rate;
                                            world_map[sx, sy].water_units -= actual_rate;
                                            // check new source cell: assign air if needed
                                            if (world_map[sx, sy].water_units == 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy].pressure = 0;
                                            }

                                            break;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (valid_array(sx, sy - 1) && world_map[sx, sy - 1].water_units == 0)// if there is nothing above source
                                {
                                    // rule 2.
                                    continue;
                                }
                                else // break needed?
                                {
                                    break;
                                }
                            }
                            else if (actual_rate == 0 && world_map[sx, sy].water_units != 100) // ignore transfer
                            {
                                if (world_map[sx, sy].water_units == 0)
                                {
                                    world_map[sx, sy].tile_id = 0;
                                    world_map[sx, sy].pressure = 0;
                                }
                                // no break in this part
                                continue;
                            }
                            else if (actual_rate > 0) // [STANDARD TRANSFER [1-preferred]] -- currently testing smooth flow
                            {
                                if (world_map[sx, sy].water_units <= 100
                                    && world_map[tx, ty].water_units < 100
                                    && world_map[sx, sy].water_units - actual_rate >= world_map[tx, ty].water_units + actual_rate)
                                {
                                    // RULES for target:
                                    bool rule1 = false; // 1. water with water below
                                    bool rule2 = false; // 2. air with solid below and vertical transfer didn't happen
                                    bool rule3 = false; // 3. air with 100w below and vertical transfer didn't happen
                                    bool rule4 = false; // 4. right ledge = source with solid below, target is air

                                    if (valid_array(tx, ty + 1)
                                        && world_map[tx, ty].tile_id == -1
                                        && world_map[tx, ty + 1].tile_id != 0)
                                        rule1 = true;
                                    else if (world_map[tx, ty].tile_id == 0 && valid_array(tx, ty + 1))
                                    {
                                        if (world_map[tx, ty + 1].tile_id > 0) //bottom cell is solid
                                            rule2 = true;
                                        else if (world_map[tx, ty + 1].tile_id == -1 && world_map[tx, ty + 1].water_units > 90) // overflow for almost filled cells
                                            rule3 = true;
                                    }
                                    //ledge (rule4 changed to properly account for 1 cell before target - ledge.
                                    if (valid_array(tx - 1, ty + 1) // one before ledge
                                        && world_map[tx - 1, ty + 1].tile_id > 0 // one before ledgte must be a solid

                                        && valid_array(tx, ty + 1) // one just over the ledge
                                        && world_map[tx, ty + 1].tile_id <= 0) // water/air
                                    {
                                        rule4 = true;
                                    }

                                    //transfer here
                                    if (rule1 || rule2 || rule3 || rule4)
                                    {
                                        if (world_map[sx, sy].water_units - actual_rate >= world_map[tx, ty].water_units + actual_rate) // only if there is no overflow
                                        {
                                            world_map[sx, sy].water_units -= actual_rate;
                                            world_map[tx, ty].water_units += actual_rate;

                                            if (world_map[tx, ty].tile_id == 0)
                                                world_map[tx, ty].tile_id = -1;               // if target is air it will become water, solids are filtered out before this point

                                            // check new source cell: assign air if needed
                                            if (world_map[sx, sy].water_units == 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy].pressure = 0;
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }// end offset loop
                    }// horizontal flow end

                    //FINAL pressure attempt before giving up
                    // check current cell for source 
                    bool pressure_source = false;
                    bool pressure_target = false;
                    int ptx = 0;
                    int pty = 0;

                    if (valid_array(x, y))
                    {
                        if (is_top_of_the_column(x, y) /*&& valid_array(x + 1, y) && world_map[x + 1, y].tile_id > 0*/ && world_map[x, y].tile_id == -1)
                            pressure_source = true;
                        // if source is found
                        if (pressure_source)
                        {
                            // find bottom of source's column
                            int upper_limit = y;
                            int lower_limit = find_bottom_of_column(x, y);

                            for (int i = lower_limit; i >= upper_limit; i--)// go from bottom to top (i is scan value for current depth)
                            {
                                for (int j = x; x < w; j++) // move to the right (j is the X value for target cell)
                                {
                                    // find top of the column 
                                    int target_y = find_top_of_the_column_index(j, i);
                                    // check target
                                    /*
                                    target:
                                    1. must not be adjacent column to the source
                                    2. must have a solid cell blockage to the left (any column in between source/target)
                                    3. must not be on the same height as current level being checked in scan loop
                                    4. must not be full. if it is full - if there  is air above make it new target (if it's on or below source's level)
                                    */
                                    bool rule1 = false;
                                    bool rule2 = false; // between current j and x there must be a solid cell at target height 
                                    bool rule3 = false;
                                    bool rule4 = false;

                                    if (world_map[j, target_y].water_units < 100)
                                    {
                                        rule4 = true;
                                    }
                                    else if (world_map[j, target_y].water_units == 100)
                                    {
                                        if (world_map[j, target_y - 1].tile_id == 0)
                                        {
                                            target_y -= 1;
                                            rule4 = true;
                                        }
                                    }

                                    if (j - x > 1)
                                        rule1 = true;

                                    if (is_blocked(x, j, target_y))
                                        rule2 = true;

                                    if (target_y <= i)
                                        rule3 = true;


                                    // if not good target - continue this loop until solid is found
                                    if (rule1 && rule2 && rule3 && rule4)
                                    {
                                        pressure_target = true;
                                        ptx = j;
                                        pty = target_y;

                                        break;
                                    }
                                    else
                                    {
                                        if (valid_array(j + 1, i) && world_map[j + 1, i].tile_id == -1)
                                            continue;
                                        else
                                            break;
                                    }
                                }
                                // exit main loop
                                if (!pressure_target)
                                    break;
                            }
                            // check if target exists, if not - do nothing
                            if (pressure_target)
                            {
                                int psx = x;
                                int psy = y;
                                // transfer between source and target

                                // different heights
                                if (psy < pty)
                                {
                                    if (world_map[psx, psy].water_units > 0 && world_map[ptx, pty].water_units < 100)
                                    {
                                        world_map[psx, psy].water_units--;
                                        world_map[ptx, pty].water_units++;
                                        world_map[ptx, pty].tile_id = -1;
                                    }
                                    else// target is 100
                                    {
                                        // start a new cell
                                        if (world_map[ptx, pty - 1].tile_id <= 0)
                                        {
                                            world_map[psx, psy].water_units--;
                                            world_map[ptx, pty - 1].water_units++;
                                            world_map[ptx, pty - 1].tile_id = -1;
                                        }
                                    }

                                    if (world_map[psx, psy].water_units == 0)
                                    {
                                        world_map[psx, psy].tile_id = 0;
                                    }
                                }
                                //same height
                                else if (psy == pty)
                                {
                                    if (world_map[psx, psy].water_units > 0 && world_map[ptx, pty].water_units < 100
                                        && world_map[psx, psy].water_units - 1 >= world_map[ptx, pty].water_units + 1)
                                    {
                                        world_map[psx, psy].water_units--;
                                        world_map[ptx, pty].water_units++;
                                        world_map[ptx, pty].tile_id = -1;

                                        if (world_map[psx, psy].water_units == 0)
                                        {
                                            world_map[psx, psy].tile_id = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // ===========================
                }//main for loop
            }// main for loop
        }// function end

        /// <summary>
        /// Determines if there is a ground block between cells on the same height level
        /// </summary>
        /// <param name="x1">x coordinate of leftmost cell</param>
        /// <param name="x2">x coordinate of rightmost cell</param>
        /// <param name="y">y coordinate for both cells</param>
        /// <returns></returns>
        public bool is_blocked(int x1, int x2, int y)
        {
            for (int i = x2; i > x1; i--)
            {
                if (world_map[i, y].tile_id > 0)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Flow independent function - determines if this cell is real top of the column for pressure calculations
        /// </summary>
        /// <param name="x">horizontal coordinate</param>
        /// <param name="y">vertical coordinate</param>
        /// <returns>bool value representing top of the column truth value</returns>
        public bool is_top_of_the_column(int x, int y)
        {
            if (valid_array(x, y))
            {
                if (world_map[x, y].water_units == 100)
                {
                    if (valid_array(x, y - 1) && world_map[x, y - 1].tile_id >= 0 && valid_array(x, y + 1) && (world_map[x, y + 1].tile_id > 0 || world_map[x, y + 1].water_units == 100))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (world_map[x, y].water_units > 0 && valid_array(x, y + 1) && (world_map[x, y + 1].tile_id > 0 || world_map[x, y + 1].water_units == 100))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
                return false;
        }

        public int find_bottom_of_column(int x, int y)
        {
            int offset = 0;
            for (offset = 0; offset >= 0; offset++)
            {
                if (valid_array(x, y + offset + 1) && world_map[x, y + offset + 1].tile_id == -1) // next cell is valid and water
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            return y + offset;
        }

        public int find_top_of_the_column_index(int x, int y)
        {
            int offset = 0;
            for (offset = 0; offset >= 0; offset++)
            {
                if (valid_array(x, y - offset - 1) && world_map[x, y - offset - 1].tile_id == -1 && world_map[x, y - offset].water_units == 100) // next cell is valid and water and current is full
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            return y - offset;
        }
        // maximum allowed transfer rate between selected cells
        public int calculate_transfer_rate(int sx, int sy, int tx, int ty, int adjustment = 0)
        {
            return ((world_map[sx, sy].water_units + adjustment) - world_map[tx, ty].water_units) / 2;
        }
        // reset updated flags
        public void reset_water()
        {
            for (int y = 0; y <= 100; y++)
            {
                for (int x = 0; x <= 100; x++)
                {
                    // prime variables for next run
                    if (valid_array(x, y))
                    {
                        world_map[x, y].flow = false;
                    }

                    if (valid_array(x, y + 1))
                    {
                        world_map[x, y + 1].flow = false;
                    }
                }
            }
        }
        // generate a light, in player-specified cell. (unless it is a default light)
        // player/map editor input
        public void generate_light_source(Color color, MouseState m, Engine engine, int radius, float intensity)
        {
            Vector2 cell = get_current_hovered_cell(m, engine);
            if (!is_light_object_in_cell(cell))
            {
                world_lights.Add(new PointLight(cell, color, get_tile_center(cell), radius, intensity));
                // create a light sphere
                world_lights.Last().create_light_sphere(radius, color, intensity);
            }
        }
        // automatic
        public void add_light_source(Color color, Vector2 cell, int radius, float intensity)
        {
            world_lights.Add(new PointLight(cell, color, get_tile_center(cell), radius, intensity));
            // create a light sphere
            world_lights.Last().create_light_sphere(radius, color, intensity);
        }
        // detect a light in the cell
        public bool is_light_object_in_cell(Vector2 cell)
        {
            foreach (PointLight pl in world_lights)
            {
                if (pl.cell == cell)
                {
                    return true;
                }
            }

            return false;
        }
        // Update light_spheres
        public void update_light_spheres(int frames)
        {
            foreach (PointLight pl in world_lights)
            {
                pl.update(frames);
            }
        }
        // get world'engine information
        public int length
        {
            get { return w; }
        }
        public int height
        {
            get { return h; }
        }
        public int tilesize
        {
            get { return tile_size; }
        }
        public String worldname
        {
            get { return world_name; }
        }
        // does the Tile exist in the cell
        public bool tile_exists(int x, int y) // cell coordinates
        {
            // contain in boundaries
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal length/height of map due to arrays starting at 0
                return false;
            // ui_elements exists
            if (!world_map[x - 1, y - 1].is_air())
                return true;
            else
                return false;
        }
        public bool not_tile_exists(int x, int y) // cell coordinates
        {
            // contain in boundaries
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal length/height of map due to arrays starting at 0
                return true;
            // ui_elements exists
            if (world_map[x - 1, y - 1].is_air())
                return true;
            else
                return false;
        }
        // vector variant
        public bool tile_exists(Vector2 cell) // cell coordinates
        {
            // contain in boundaries
            if (cell.X <= 0 || cell.Y <= 0 || cell.X > w || cell.Y > h) //coordinates can't be 0 or equal length/height of map due to arrays starting at 0
                return false;
            // ui_elements exists
            if (!world_map[(int)cell.X - 1, (int)cell.Y - 1].is_air())
                return true;
            else
                return false;
        }
        // edit mode functions
        public void toggle_edit_mode()
        {
            edit_mode = !edit_mode;
        }
        public bool in_edit_mode()
        {
            return edit_mode;
        }
        // add a Tile to the map
        public bool generate_tile(short id, int x, int y, Engine engine, int vol = 0)
        {
            if (this.not_tile_exists(x, y) || engine.get_editor().cell_overwrite_mode())
            {
                if (valid_cell(x, y))
                {
                    world_map[x - 1, y - 1].tile_id = id; // water = -1
                    world_map[x - 1, y - 1].water_units = vol; // 0-100
                    updated_cells.Push(new Vector2(x, y)); // push new cell on to updates stack
                }
            }

            return true;
        }
        /// <summary>
        /// Generates a new ui_elements in a specific brush radius
        /// </summary>
        /// <param name="id">tile id</param>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        /// <param name="radius">brush size 1-N </param>
        public void generate_tile(short id, int x, int y, Engine engine, int radius, int vol = 0)
        {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    // calculate sqrt of (i^2+j^2)
                    double distance_index = Math.Sqrt(i * i + j * j);
                    // determine if index is less or more than given radius. If it is - then coordinate is within proposed pixelated circle
                    if (distance_index <= radius)
                    {
                        generate_tile(id, x + i, y + j, engine, vol);
                        updated_cells.Push(new Vector2(x + i, y + j)); // push new cell on to updates stack
                    }
                }
            }
        }

        public void generate_matrix(List<Vector2> list, Engine engine, short tile_id, int vol = 0)
        {
            foreach (Vector2 cell in list)
            {
                generate_tile(tile_id, (int)cell.X, (int)cell.Y, engine, vol);
                updated_cells.Push(new Vector2(cell.X, cell.Y));
            }
        }
        public void generate_hollow_matrix(List<Vector2> list, Engine engine, short tile_id)
        {
            try // this function can potentially deal with null objects
            {
                Vector2[] sc = engine.get_editor().get_selection_real_start_end_cells(); // get adjusted start/end cells in current selection matrix
                foreach (Vector2 cell in list)
                {
                    if (cell.X == sc[0].X || cell.X == sc[1].X || cell.Y == sc[0].Y || cell.Y == sc[1].Y) // x or y equal to selection start end
                    {
                        generate_tile(tile_id, (int)cell.X, (int)cell.Y, engine);
                        updated_cells.Push(new Vector2(cell.X, cell.Y));
                    }
                }
            }
            catch (NullReferenceException) { }; // do nothing
        }
        public void erase_hollow_matrix(List<Vector2> list, Engine engine)
        {
            try // this function can potentially deal with null objects
            {
                Vector2[] sc = engine.get_editor().get_selection_real_start_end_cells(); // get adjusted start/end cells in current selection matrix
                foreach (Vector2 cell in list)
                {
                    if (cell.X == sc[0].X || cell.X == sc[1].X || cell.Y == sc[0].Y || cell.Y == sc[1].Y) // x or y equal to selection start end
                    {
                        erase_tile((int)cell.X, (int)cell.Y, engine);
                        updated_cells.Push(new Vector2(cell.X, cell.Y));
                    }
                }
            }
            catch (NullReferenceException) { }; // do nothing
        }
        public void erase_matrix(List<Vector2> list, Engine engine)
        {
            foreach (Vector2 cell in list)
            {
                erase_tile((int)cell.X, (int)cell.Y, engine);
                updated_cells.Push(new Vector2(cell.X, cell.Y));
            }
        }
        /// <summary>
        /// new
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="engine"></param>
        /// <param name="radius"></param>
        public void preview_tile(int x, int y, Engine engine, int radius, modes current)
        {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    // calculate sqrt of (i^2+j^2)
                    double distance_index = Math.Sqrt(i * i + j * j);
                    // determine if index is less or more than given radius. If it is - then coordinate is within proposed pixelated circle
                    if (distance_index <= radius)
                    {
                        preview(x + i, y + j, engine, current);
                    }
                }
            }
        }

        public bool preview(int x, int y, Engine engine, modes current)
        {
            if (this.not_tile_exists(x, y) || engine.get_editor().cell_overwrite_mode() || current == modes.delete)
            {
                if (valid_cell(x, y))
                {
                    Rectangle current_cell_dimensions = get_cell_rectangle_on_screen(engine, new Vector2(x, y));
                    Texture2D temp = null;
                    if (current == modes.add)
                    {
                        temp = Tile.find_tile(get_current_edit_tile(engine));
                    }
                    else if (current == modes.delete)
                    {
                        temp = engine.get_texture("deleted_indicator");
                    }

                    engine.xna_draw(temp,
                    new Vector2(current_cell_dimensions.X, current_cell_dimensions.Y), // origin of the line
                    new Rectangle(0, 0, tile_size, tile_size),                                                              // rectangle crop
                    Color.White * 0.5f, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                }
            }

            return true;
        }
        //remove Tile from map (except the bottom level)
        public bool erase_tile(int x, int y, Engine engine)
        {
            if (valid_cell(engine) && this.tile_exists(x, y))
            {
                world_map[x - 1, y - 1].tile_id = 0;
                world_map[x - 1, y - 1].water_units = 0;
                updated_cells.Push(new Vector2(x, y)); // push deleted cell on to updates stack
            }
            return true;
        }

        public void erase_tile(int x, int y, Engine engine, int radius)
        {
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    // calculate sqrt of (i^2+j^2)
                    double distance_index = Math.Sqrt(i * i + j * j);
                    // determine if index is less or more than given radius. If it is - then coordinate is within proposed pixelated circle
                    if (distance_index <= radius)
                    {
                        // validate index values before passing them to array
                        int index_x = x + i - 1;
                        int index_y = y + j - 1;
                        // delete cells
                        if (index_x >= 0 && index_x < w
                           && index_y >= 0 && index_y < h) // within horizontal and vertical borders
                        {
                            world_map[index_x, index_y].tile_id = 0;
                            world_map[index_x, index_y].water_units = 0;
                            updated_cells.Push(new Vector2(x + i, y + j)); // push deleted cell on to updates stack
                        }
                    }
                }
            }
        }
        // display the map (DRAW_map function display all terrain and additional props: lights, water, weather etc.

        // draw edit overlay Draw
        /*public void draw_world_editor_UI(Engine engine,SpriteBatch sb)
        {            
            // world bounds
            draw_world_bounds(engine, sb);     
            // draw editor Draw
            if (edit_mode)
            {
                editor.Draw(sb,engine, this); // displays editor menu and interface
            }     
        }*/
        /// <summary>
        /// Draws world border lines and tool lines - everything that should not be masked in shader
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="sb">spritebatch</param>
        public void draw_world_geometry(Engine engine, SpriteBatch sb)
        {
            draw_world_bounds(engine, sb);
        }
        /// <summary>
        /// Draws world user interface to a render surface without applying any masking effects
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="sb">spritebatch</param>
        public void draw_world_UI_static(Engine engine, SpriteBatch sb)
        {
            if (edit_mode)
            {
                engine.get_editor().draw_static_containers(sb, engine, this); // displays editor menu and interface
            }
        }

        public void draw_world_UI_context_and_tooltips(Engine engine, SpriteBatch sb)
        {
            if (edit_mode)
            {
                engine.get_editor().draw_context_containers_and_tooltips(sb, engine, this); // displays editor menu and interface
            }
        }
        /// <summary>
        /// Draws a masking layer to a separate render surface and then combines with the UI layer using negative masking
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="sb">spritebatch</param>
        public void draw_world_mask_layer(Engine engine, SpriteBatch sb)
        {
            if (edit_mode)
            {
                engine.get_editor().draw_masking_layer();
            }
        }

        public void draw_world_post_processing(Engine engine, SpriteBatch sb)
        {
            if (edit_mode)
            {
                engine.get_editor().draw_post_processing(engine, sb);
            }
        }
        // execute editor element_command
        public void execute_command(command c, Engine engine, Game1 game)
        {
            /* if (!editor.get_overall_hover_status())
                 editor.interface_gameplay(editor_command, this, engine);
             else if(editor.get_overall_hover_status())
                 editor.interface_UI(editor_command, game);*/

            // execute GUI editor_command
            engine.get_editor().editor_command(engine, c);
        }
        // current edit Tile
        public short get_current_edit_tile(Engine engine)
        {
            return engine.get_editor().get_current_editor_cell();
        }
        // scroll through ui_elements up
        /*public void increase_edit_tile()
        {
            if (edit_tile_number < Tile.tile_list.Count)
                edit_tile_number++;
        }
// scroll through ui_elements down
        public void decrease_edit_tile()
        {
            if (edit_tile_number > 1)
                edit_tile_number--;
        }*/
        // determine if the cell exists on the map, in other words - falls withn world bounds (mouse hover)
        public bool valid_cell(Engine engine)
        {
            Vector2 hover_cell = this.get_current_hovered_cell(engine.get_current_mouse_state(), engine); // get_current_hovered_cell calculates cell numbers, not coordinates

            if (hover_cell.X > 0 && hover_cell.X <= length && hover_cell.Y > 0 && hover_cell.Y <= height)
                return true;

            return false;
        }
        public bool valid_cell(int x, int y) //(coordinates)
        {
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal length/height of map due to arrays starting at 0
                return false;
            else
                return true;
        }
        public bool valid_array(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
                return false;
            else
                return true;
        }
        public bool valid_cell(Vector2 v) //(coordinates)
        {
            if (v.X <= 0 || v.Y <= 0 || v.X > w || v.Y > h) //coordinates can't be 0 or equal length/height of map due to arrays starting at 0
                return false;
            else
                return true;
        }
        // calculate tile center coordinates in the world - without adjusting for camera
        public Vector2 get_tile_center(Vector2 cell)
        {
            int x = ((int)cell.X * tile_size) - (tile_size / 2);
            int y = ((int)cell.Y * tile_size) - (tile_size / 2);

            return new Vector2(x, y);
        }
        public Vector2 get_tile_origin(Vector2 cell)
        {
            int x = ((int)cell.X * tile_size) - tile_size;
            int y = ((int)cell.Y * tile_size) - tile_size;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get rectangle adjusted for camera. Coordinates as seen on screen, not as they would be in memory without camera adjustments
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public Rectangle get_cell_rectangle_on_screen(Engine engine, Vector2 cell)
        {
            Rectangle temp = new Rectangle();
            temp.X = ((int)cell.X * tile_size) - tile_size - (int)engine.get_camera_offset().X;
            temp.Y = ((int)cell.Y * tile_size) - tile_size - (int)engine.get_camera_offset().Y;
            temp.Width = temp.Height = tile_size;

            return temp;
        }

        /*public Color get_grid_color()
        {
            return grid_color;
        }*/
        // determine if the Tile is at least partly visible on-screen
        public bool is_tile_visible(Vector2 cell, Engine engine)
        {
            // camera origin 
            int minimal_visible_cell_x = ((int)engine.get_camera_offset().X / (int)tile_size);
            int minimal_visible_cell_y = ((int)engine.get_camera_offset().Y / (int)tile_size);
            // viewport dimensions
            int max_visible_cell_x = minimal_visible_cell_x + (engine.get_viewport().Width / tile_size);
            int max_visible_cell_y = minimal_visible_cell_y + (engine.get_viewport().Height / tile_size);

            if (minimal_visible_cell_x < 1)
                minimal_visible_cell_x = 1;
            else if (minimal_visible_cell_x > w)
                minimal_visible_cell_x = w;

            if (max_visible_cell_x < 1)
                max_visible_cell_x = 1;
            else if (max_visible_cell_x > w)
                max_visible_cell_x = w;

            if (minimal_visible_cell_y < 1)
                minimal_visible_cell_y = 1;
            else if (minimal_visible_cell_y > h)
                minimal_visible_cell_y = h;

            if (max_visible_cell_y < 1)
                max_visible_cell_y = 1;
            else if (max_visible_cell_y > h)
                max_visible_cell_y = h;
            // condition (add/subtract 1 to account for semi-visible ui_elements)
            if (cell.X >= minimal_visible_cell_x - 1
                && cell.X <= max_visible_cell_x + 1
                && cell.Y >= minimal_visible_cell_y - 1
                && cell.Y <= max_visible_cell_y + 1)
                return true;

            return false;
        }
        public Vector2 max_visible_tile(Engine engine)
        {
            // camera origin 
            int minimal_visible_cell_x = ((int)engine.get_camera_offset().X / (int)tile_size);
            int minimal_visible_cell_y = ((int)engine.get_camera_offset().Y / (int)tile_size);
            // viewport dimensions
            int max_visible_cell_x = minimal_visible_cell_x + (engine.get_viewport().Width / tile_size);
            int max_visible_cell_y = minimal_visible_cell_y + (engine.get_viewport().Height / tile_size);

            if (minimal_visible_cell_x < 1)
                minimal_visible_cell_x = 1;
            else if (minimal_visible_cell_x > w)
                minimal_visible_cell_x = w;

            if (max_visible_cell_x < 1)
                max_visible_cell_x = 1;
            else if (max_visible_cell_x > w)
                max_visible_cell_x = w;

            if (minimal_visible_cell_y < 1)
                minimal_visible_cell_y = 1;
            else if (minimal_visible_cell_y > h)
                minimal_visible_cell_y = h;

            if (max_visible_cell_y < 1)
                max_visible_cell_y = 1;
            else if (max_visible_cell_y > h)
                max_visible_cell_y = h;

            return new Vector2(max_visible_cell_x, max_visible_cell_y);
        }
        public Vector2 min_visible_tile(Engine engine)
        {
            // camera origin 
            int minimal_visible_cell_x = ((int)engine.get_camera_offset().X / (int)tile_size);
            int minimal_visible_cell_y = ((int)engine.get_camera_offset().Y / (int)tile_size);

            if (minimal_visible_cell_x < 1)
                minimal_visible_cell_x = 1;
            else if (minimal_visible_cell_x > w)
                minimal_visible_cell_x = w;

            if (minimal_visible_cell_y < 1)
                minimal_visible_cell_y = 1;
            else if (minimal_visible_cell_y > h)
                minimal_visible_cell_y = h;

            return new Vector2(minimal_visible_cell_x, minimal_visible_cell_y);
        }
        // return sky color
        public Color get_sky_color()
        {
            return sky;
        }

        // returns number of filled tiles - Profiler report: large amount of work done here 
        public int get_total_tiles_filled()
        {
            int count = 0;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (world_map[i, j].tile_id > 0)
                        count++;
                }
            }
            return count;
        }

        public int get_world_size()
        {
            return w * h;
        }

        public float get_percent_filled()
        {
            return (float)get_total_tiles_filled() / (float)(w * h);
        }
        // draw a box surrounding the world
        public void draw_world_bounds(Engine engine, SpriteBatch sb)
        {

            //draw lines (rectangle (0,0,length of line, height of line)
            if (edit_mode && !engine.get_editor().gui_move_mode)
            {
                engine.xna_draw(Engine.pixel, // top
                    map_origin - engine.get_camera_offset(), // origin of the line
                    new Rectangle(0, 0, tile_size * length, 1),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // bottom
                    map_origin - engine.get_camera_offset() + new Vector2(0, tile_size * height), // origin of the line
                    new Rectangle(0, 0, tile_size * length, 1),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // left
                    map_origin - engine.get_camera_offset(), // origin of the line
                    new Rectangle(0, 0, 1, tile_size * height),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // right
                    map_origin - engine.get_camera_offset() + new Vector2(tile_size * length, 0), // origin of the line
                    new Rectangle(0, 0, 1, tile_size * height),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }
            // draw the rest of it
            if (edit_mode && engine.get_editor().GUI.hover_detect() == false && !engine.get_editor().gui_move_mode)
            {
                // calculate ui_elements only visible on screen
                int min_cell_x = (int)min_visible_tile(engine).X - 1; int min_cell_y = (int)min_visible_tile(engine).Y - 1;
                int max_cell_x = (int)max_visible_tile(engine).X; int max_cell_y = (int)max_visible_tile(engine).Y;

                // Grid lines - update: only  draw grid for hovered cell
                float line_transparency = engine.grid_transparency_value; // used to highlight every 10th line
                float transparency_bg = 0.05f;

                if (engine.get_editor().line_start_cell.X > -1) // line tool active
                {// vertical
                    for (int i = min_cell_x; i <= max_cell_x; i++)
                    {
                        Vector2 position = new Vector2(i * tilesize, min_cell_y * tilesize) - engine.get_camera_offset();
                        if (i == engine.get_editor().line_start_cell.X || i == engine.get_editor().line_end_cell.X)
                        {
                            engine.xna_draw(Engine.pixel, position - new Vector2(tile_size / 2, 0), new Rectangle(0, 0, 1, tile_size * (max_cell_y - min_cell_y)), engine.get_grid_color() * line_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            engine.xna_draw(Engine.pixel, position - new Vector2(tile_size, 0), new Rectangle(0, 0, tile_size, tile_size * (max_cell_y - min_cell_y)), engine.get_grid_color() * transparency_bg, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                    }
                    // horizontal
                    for (int i = min_cell_y; i <= max_cell_y; i++)
                    {
                        Vector2 position = new Vector2(min_cell_x * tilesize, i * tilesize) - engine.get_camera_offset();
                        if (i == engine.get_editor().line_start_cell.Y || i == engine.get_editor().line_end_cell.Y)
                        {
                            engine.xna_draw(Engine.pixel, position - new Vector2(0, tile_size / 2), new Rectangle(0, 0, tile_size * (max_cell_x - min_cell_x), 1), engine.get_grid_color() * line_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            engine.xna_draw(Engine.pixel, position - new Vector2(0, tile_size), new Rectangle(0, 0, tile_size * (max_cell_x - min_cell_x), tile_size), engine.get_grid_color() * transparency_bg, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                    }
                }
                else // regular mouse pointer
                {// vertical
                    for (int i = min_cell_x; i <= max_cell_x; i++)
                    {
                        Vector2 position = new Vector2(i * tilesize, min_cell_y * tilesize) - engine.get_camera_offset();
                        if (i == get_current_hovered_cell(engine.get_current_mouse_state(), engine).X)
                        {
                            engine.xna_draw(Engine.pixel, position - new Vector2(tile_size / 2, 0), new Rectangle(0, 0, 1, tile_size * (max_cell_y - min_cell_y)), engine.get_grid_color() * line_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            engine.xna_draw(Engine.pixel, position - new Vector2(tile_size, 0), new Rectangle(0, 0, tile_size, tile_size * (max_cell_y - min_cell_y)), engine.get_grid_color() * transparency_bg, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                    }
                    // horizontal
                    for (int i = min_cell_y; i <= max_cell_y; i++)
                    {
                        Vector2 position = new Vector2(min_cell_x * tilesize, i * tilesize) - engine.get_camera_offset();
                        if (i == get_current_hovered_cell(engine.get_current_mouse_state(), engine).Y)
                        {
                            engine.xna_draw(Engine.pixel, position - new Vector2(0, tile_size / 2), new Rectangle(0, 0, tile_size * (max_cell_x - min_cell_x), 1), engine.get_grid_color() * line_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            engine.xna_draw(Engine.pixel, position - new Vector2(0, tile_size), new Rectangle(0, 0, tile_size * (max_cell_x - min_cell_x), tile_size), engine.get_grid_color() * transparency_bg, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                    }
                }
            }
        }
        // detects which cell was clicked/hovered in (returns cell numbers)
        public Vector2 get_current_hovered_cell(MouseState state, Engine engine)
        {
            int horizontal_cell = 0;
            int vertical_cell = 0;
            // get x and y relative to the map
            int x = state.X + (int)engine.get_camera_offset().X;
            int y = (int)map_origin.Y + (int)engine.get_camera_offset().Y + state.Y;

            if (x > 0 && x < tile_size * length && y > 0 && y < tile_size * height)
            {
                horizontal_cell = x / tile_size + 1;
                vertical_cell = y / tile_size + 1;
            }
            else
            {
                horizontal_cell = -1;
                vertical_cell = -1;
            }

            return new Vector2(horizontal_cell, vertical_cell);
        }
        //
        /*public void draw_edit_tile(Engine engine, Vector2 position)
        {
            engine.xna_draw(
                        Tile.find_tile(edit_tile_number), // find a texture specified by Tile_id in tile_map -> Tile_id_Listing -> Tile_id
                        position, // where
                        new Rectangle(180,0,tile_size,tile_size), Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.2f);
            engine.add_draw_calls(1);
        }*/
        // identify the Tile in a provided cell
        public short find_tile_id_of_cell(int X, int Y)
        {
            if (valid_cell(X, Y))
                return world_map[X - 1, Y - 1].tile_id;
            else
                return 32767; // return tile id of the world edge
        }
        public short find_tile_id_of_cell(Vector2 cell)
        {
            return world_map[(int)cell.X - 1, (int)cell.Y - 1].tile_id;
        }
        // wrapping function: delete all ui_elements of the same contexttype as the one hovered (deletes a column)        
        public void delete_group(Engine engine)
        {
            Vector2 start = get_current_hovered_cell(engine.get_current_mouse_state(), engine);
            delete_group_of_tiles(start, start, engine);
        }
        // adding ui_elements
        public void add_group(Engine engine)
        {
            Vector2 start = get_current_hovered_cell(engine.get_current_mouse_state(), engine);
            add_group_of_tiles(start, start, engine);
        }
        // delete a group of ui_elements ( version 1 - deletes every directly connected Tile of the same contexttype) 
        private void delete_group_of_tiles(Vector2 current, Vector2 original, Engine engine)
        {
            int X = (int)current.X;
            int Y = (int)current.Y;
            int id = find_tile_id_of_cell(X, Y);

            int diff_X = ((int)original.X > X) ? (int)original.X - X : X - (int)original.X;
            int diff_Y = ((int)original.Y > Y) ? (int)original.Y - Y : Y - (int)original.Y;

            // break execution of the function if the square being filled is bigger than 100 ui_elements across
            if (diff_X > 25 || diff_Y > 25)
                return;

            erase_tile(X, Y, engine);

            // find connected ui_elements
            // directly above
            if (tile_exists(X, Y - 1) && id == find_tile_id_of_cell(X, Y - 1))
            {
                delete_group_of_tiles(current - new Vector2(0, 1), original, engine); // recur to the top Tile to repeat deletion process
            }
            // directly below
            if (tile_exists(X, Y + 1) && id == find_tile_id_of_cell(X, Y + 1))
            {
                delete_group_of_tiles(current + new Vector2(0, 1), original, engine); // recur to the bottom Tile to repeat deletion process
            }
            // to the right
            if (tile_exists(X + 1, Y) && id == find_tile_id_of_cell(X + 1, Y))
            {
                delete_group_of_tiles(current + new Vector2(1, 0), original, engine); // recur to the right Tile to repeat deletion process
            }
            // to the left
            if (tile_exists(X - 1, Y) && id == find_tile_id_of_cell(X - 1, Y))
            {
                delete_group_of_tiles(current - new Vector2(1, 0), original, engine); // recur to the left Tile to repeat deletion process
            }

            return;
        }
        // add a group of ui_elements 
        private void add_group_of_tiles(Vector2 current, Vector2 original, Engine engine)
        {
            int X = (int)current.X;
            int Y = (int)current.Y;
            int diff_X = ((int)original.X > X) ? (int)original.X - X : X - (int)original.X;
            int diff_Y = ((int)original.Y > Y) ? (int)original.Y - Y : Y - (int)original.Y;

            if (diff_X > 25 || diff_Y > 25)
                return;

            generate_tile(get_current_edit_tile(engine), X, Y, engine);
            // find connected ui_elements
            if (find_tile_id_of_cell(X, Y + 1) == 0) // directly below
            {
                add_group_of_tiles(current + new Vector2(0, 1), original, engine);
            }
            if (find_tile_id_of_cell(X + 1, Y) == 0) // to the right
            {
                add_group_of_tiles(current + new Vector2(1, 0), original, engine);
            }
            if (find_tile_id_of_cell(X, Y - 1) == 0) // directly above
            {
                add_group_of_tiles(current - new Vector2(0, 1), original, engine);
            }
            if (find_tile_id_of_cell(X - 1, Y) == 0) // to the left
            {
                add_group_of_tiles(current - new Vector2(1, 0), original, engine);
            }

            return;
        }
        // calculate a rectangle for spritesheet
        private void update_rectangle(int variant, Vector2 cell)
        {
            int column, row;
            if (variant == 10)
            {
                column = 9;
                row = 0;
            }
            else
            {
                column = variant % 10 - 1;
                row = variant / 10;
            }

            world_map[(int)cell.X - 1, (int)cell.Y - 1].tile_rec = new Rectangle(column * 20, row * 20, 20, 20);
        }
        // calculate a corner rectangle for spritesheet
        private void update_corner(int variant, Vector2 cell, int corner_number)
        {
            int column, row;
            if (variant == 10)
            {
                column = 9;
                row = 0;
            }
            else
            {
                column = variant % 10 - 1;
                row = variant / 10;
            }

            world_map[(int)cell.X - 1, (int)cell.Y - 1].corners[corner_number] = new Rectangle(column * 20, row * 20, 20, 20);
        }
        // determine connection config
        private void tile_connections(Vector2 cell)
        {
            bool top, bottom, right, left; // connections
            bool top_right, top_left, bottom_right, bottom_left;

            top = tile_exists(cell + new Vector2(0, -1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(0, -1)))*/;
            bottom = tile_exists(cell + new Vector2(0, 1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(0, 1)))*/;
            right = tile_exists(cell + new Vector2(1, 0)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(1, 0)))*/;
            left = tile_exists(cell + new Vector2(-1, 0)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(-1, 0)))*/;

            top_right = tile_exists(cell + new Vector2(1, -1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(1, -1)))*/;
            top_left = tile_exists(cell + new Vector2(-1, -1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(-1, -1)))*/;
            bottom_right = tile_exists(cell + new Vector2(1, 1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(1, 1)))*/;
            bottom_left = tile_exists(cell + new Vector2(-1, 1)) /*&& (find_tile_id_of_cell(cell) == find_tile_id_of_cell(cell + new Vector2(-1, 1)))*/;
            // 0 connections
            if (!top && !bottom && !right && !left)
            {
                update_rectangle(10, cell);
            }
            // 1 connection    
            else if (top && !bottom && !right && !left) // top
            {
                update_rectangle(39, cell);
            }
            else if (!top && bottom && !right && !left) // bottom
            {
                update_rectangle(19, cell);
            }
            else if (!top && !bottom && right && !left) // right
            {
                update_rectangle(1, cell);
            }
            else if (!top && !bottom && !right && left) // left
            {
                update_rectangle(3, cell);
            }
            // 2 connections
            else if (!top && !bottom && right && left) // left + right
            {
                update_rectangle(2, cell);
            }
            else if (top && bottom && !right && !left) // top + bottom
            {
                update_rectangle(29, cell);
            }
            else if (top && !bottom && right && !left) // top + right
            {
                update_rectangle(31, cell);
            }
            else if (top && !bottom && !right && left) // top + left
            {
                update_rectangle(33, cell);
            }
            else if (!top && bottom && right && !left) // bottom + right
            {
                update_rectangle(11, cell);
            }
            else if (!top && bottom && !right && left) // bottom + left
            {
                update_rectangle(13, cell);
            }
            // 3 connections
            else if (!top && bottom && right && left) // all except top
            {
                update_rectangle(12, cell);
            }
            else if (top && !bottom && right && left) // all except  bottom
            {
                update_rectangle(32, cell);
            }
            else if (top && bottom && !right && left) // all except right
            {
                update_rectangle(23, cell);
            }
            else if (top && bottom && right && !left) // all except left
            {
                update_rectangle(21, cell);
            }
            // 4 connections
            else if (top && bottom && right && left) // all
            {
                update_rectangle(22, cell);
            }
            // corners
            //================
            if (!top_left && top && left)
            {
                update_corner(5, cell, 0);
            }
            else
            {
                update_corner(4, cell, 0);
            }

            if (!top_right && top && right)
            {
                update_corner(6, cell, 1);
            }
            else
            {
                update_corner(4, cell, 1);
            }

            if (!bottom_left && bottom && left)
            {
                update_corner(7, cell, 2);
            }
            else
            {
                update_corner(4, cell, 2);
            }

            if (!bottom_right && bottom && right)
            {
                update_corner(8, cell, 3);
            }
            else
            {
                update_corner(4, cell, 3);
            }
        }

        public void center_camera_on_world_origin(Engine engine)
        {
            // calculate first cell

            //center camera
            engine.set_camera_offset(Vector2.Zero);
        }

        public string get_tile_id(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return world_map[(int)source.X - 1, (int)source.Y - 1].tile_id.ToString();
            }
            else
                return "";
        }
        public float get_tile_water(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return world_map[(int)source.X - 1, (int)source.Y - 1].water_units;
            }
            else
                return 0;
        }
        public float get_tile_pressure(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return world_map[(int)source.X - 1, (int)source.Y - 1].pressure;
            }
            else
                return 0;
        }

        public Vector2 get_tile_psource(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return new Vector2(world_map[(int)source.X - 1, (int)source.Y - 1].source_x + 1, world_map[(int)source.X - 1, (int)source.Y - 1].source_y + 1);
            }
            else
                return new Vector2(-1, -1);
        }
        // calculate current sky color based on the  time and color ranges
        // this function interpolates between colors of day phases based on the time of day (in minutes - for easier calculations)
        public Color calculate_sky_color(WorldClock clock)
        {
            int time = clock.get_time_in_seconds();
            Color sky; // placeholder for a new sky color

            if (time <= 43200) // midnight - noon
                sky = Color.Lerp(sky_color[0], sky_color[1], (float)time / 43200.0f);
            else if (time > 43200 && time <= 61200) // noon - 5PM
                sky = Color.Lerp(sky_color[1], sky_color[2], ((float)time - 43200.0f) / 18000.0f);
            else // 5 pm - midnight
                sky = Color.Lerp(sky_color[2], sky_color[0], ((float)time - 61200.0f) / 25200.0f);

            return sky;
        }
        // calculates amount of ambient light based on time of day
        public float get_ambient_light(WorldClock clock)
        {
            float time = (float)clock.get_time_in_seconds();
            float ambient = 0.0f;

            // testing faster transitions 
            if (time >= 79200 && time <= 86400 || time >= 0 && time <= 18000)       // 10pm - 5am (79200 - 0 - 18000)
            {
                ambient = 0.05f; // night value
            }
            else if (time > 18000 && time <= 21600)                                 // 5 am - 6 am (18000 - 21600)
            {
                ambient = 0.05f + (0.85f * ((time - 18000.0f) / 3600.0f));
            }
            else if (time > 21600 && time <= 68400)                                  // 6 am - 7pm (21600 - 68400)
            {
                ambient = 0.9f;  // daytime value
            }
            else if (time > 68400 & time <= 72000)                                   // 7pm - 8pm (68400 - 72000)
            {
                ambient = 0.9f - (0.7f * ((time - 68400.0f) / 3600.0f));
            }
            else                                                                    // 8 PM - 10 PM (72000 - 79200)
            {
                ambient = 0.2f - (0.15f * ((time - 72000.0f) / 7200.0f));
            }

            return ambient;
        }
        // cast circle from light source
        public void world_draw_point_lights(Engine engine, SpriteBatch sb)
        {
            foreach (PointLight pl in world_lights)
            {
                engine.xna_draw(pl.light_sphere, pl.position - engine.get_camera_offset() - new Vector2(pl.active_radius() / 2, pl.active_radius() / 2), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1f);
            }
        }

        public void world_draw_point_light_sources(Engine engine)
        {
            // draw objects (light)
            int count = 0;
            foreach (PointLight pl in world_lights)
            {
                Vector2 object_offset = new Vector2(pl.sprite.Width / 2, pl.sprite.Height / 2);
                engine.xna_draw(pl.sprite, pl.position - engine.get_camera_offset() - object_offset, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.4f);
                count++;
            }
        }
    }
}

/*
//------------------------------------------
            //--Pressure transfer-----------------------Improvement: find source top and target top, if same height and 0 actual rate - attempt to find another source to the left and up
            //------------------------------------------
            int source_backtracker = 0; // value needed for source adjustment for irregular ceiling surfaces
            for (int offset = 1; offset < w; offset++) // minimal offset is 2, because there has to be a solid border between cells
            {
                sx = x - source_backtracker; sy = y;          // current source
                tx = x + offset; ty = y;  // current target
                // break conditions
                if (!valid_array(sx, sy) || !valid_array(tx, ty))
                    break;
                if (world_map[tx, ty].tile_id >= 0) // 
                    break;
                // end
                if (valid_array(sx,sy) && valid_array(tx,ty) // in bounds
                    && world_map[sx, sy].water_units == 100  // both are 100
                    && world_map[tx, ty].water_units == 100)
                {
                    // calculate rules
                    int pressure_rate = 3;
                    int actual_pressure_rate = 0;

                    int tsx = sx; int tsy = sy;
                    int ttx = tx; int tty = ty;
                    // rule 1: == 100 source and target
                    // rule 2: source has a water cell above and solid on the right of that, target has air/water above and solid to the left of that
                    bool rule_p1 = false;
                    bool rule_p2 = false;

                    if(world_map[tsx, tsy].water_units == 100 && world_map[ttx, tty].water_units == 100)
                    {
                        rule_p1 = true;
                    }

                    
                    if (valid_array(tsx, tsy - 1) && valid_array(ttx, tty - 1)) // both cells above source are in bounds
                    {// barrier rule for pressure transfer
                        if (world_map[tsx, tsy - 1].tile_id == -1         // above source: water
                            && world_map[ttx,tty - 1].tile_id <= 0        // above target: water or air (missing condition)
                            && valid_array(tsx + 1, tsy - 1)              // above source on the right - in bounds
                            && world_map[tsx + 1, tsy - 1].tile_id > 0    // above source on the right - solid
                            && valid_array(ttx - 1, tty - 1)              // above target on the left - in bounds
                            && world_map[ttx - 1, tty - 1].tile_id > 0    // above target on the left - solid
                            && offset > 1                                 // offset must be > 1 (but there must be no leakage through walls
                            )
                        {
                            rule_p2 = true;
                        }
                    }
                    // adjust for continues
                    if(source_backtracker != 0)
                    {
                        rule_p2 = true;
                    }
                    // if both rules are true - proceed with pressure transfer (right directional version)
                    if (rule_p1 && rule_p2)
                    {
                        // find top cells for both source and target
                        // top source: any water cell with solid,air or out of bounds above it
                        // top target: non-full water cell or 1st air cell found above full water cell

                    // after adjusted top column source and target have been found - either transfer or ignore
                        // find top source
                        while (valid_array(tsx, tsy) && world_map[tsx, tsy].tile_id < 0)
                        {
                            if (valid_array(tsx, tsy - 1)
                                && world_map[tsx, tsy - 1].tile_id == -1
                                && world_map[tsx, tsy].water_units == 100) // next cell is still good to be top
                            {
                                tsy--; // ascend top column cell source
                            }
                            else
                            {
                                break; // avoid infinite loop
                            }
                        }
                        // find top target
                        while (valid_array(ttx, tty) && world_map[ttx, tty].tile_id <= 0)
                        {
                            if (valid_array(ttx, tty - 1)
                                && world_map[ttx, tty].tile_id <= 0 
                                && world_map[ttx, tty].water_units == 100
                                && world_map[ttx, tty - 1].tile_id <= 0) // first non-full cell or first air cell
                            {
                                tty--; // ascend top column cell target
                            }
                            else
                            {
                                break; // avoid infinite loop
                            }
                        }
                    // break conditions 
                        if (world_map[ttx, tty].tile_id > 0) 
                            break;

                        // find a block in the ceiling TEST
                        // allow a solid if there is water on the original source level
                        if (world_map[ttx, tty].water_units == 100 && valid_array(ttx, tty - 1) && world_map[ttx, tty - 1].tile_id > 0)
                        {
                            // find a new target by moving right until solid is found
                            //  if right cell is water - go up to source height
                            //  if a good target is found make it a new target, otherwise break main loop 
                            int limit = tsy; // height of source
                            bool done = false;

                            for(int soffset = 1; soffset < w; soffset++)// go right in search of new target
                            {
                                if (valid_array(ttx + soffset, tty) && world_map[ttx + soffset, tty].tile_id == -1)
                                {
                                    for (int voffset = 0; tty - voffset >= limit; voffset++) // find top of the column
                                    {
                                        if(world_map[ttx + soffset, tty - voffset].tile_id <= 0 && // new target should be either air or water
                                           world_map[ttx + soffset, tty - voffset].water_units < 100)
                                        {
                                            //assign new target and break
                                            ttx = ttx + soffset;
                                            tty = tty - voffset;
                                            done = true;
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else // bad new target base
                                {
                                    // TESTED new irregular surface pressure condition (works)
                                    if (world_map[ttx + soffset, sy].tile_id == -1)// original source height and new target position has water (connection exists)
                                        continue;
                                    else
                                        break;

                                }

                                if(done)
                                   break;
                            }
                        }
                    // calculate transfers
                        if (tsy <= tty) // source above/same level as target (above means it has a smaller y value)
                        {
                            if (tsy < tty) // different levels 
                            {
                                actual_pressure_rate = calculate_transfer_rate(tsx, tsy, ttx, tty, 100);

                                if (actual_pressure_rate > pressure_rate)
                                    actual_pressure_rate = pressure_rate;

                                if (world_map[ttx, tty].water_units + actual_pressure_rate <= 100) // target doesn't overflow
                                {
                                    if (world_map[tsx, tsy].water_units - actual_pressure_rate < 0) // if there is not enough in current source cell for transfer to be successful
                                    {
                                        int remainder = actual_pressure_rate - world_map[tsx, tsy].water_units;

                                        world_map[tsx, tsy].water_units = 0;
                                        world_map[tsx, tsy].tile_id = 0;

                                        world_map[tsx, tsy + 1].water_units -= remainder;
                                        world_map[ttx, tty].water_units += actual_pressure_rate;
                                        // assign water cell to traget
                                        if (world_map[ttx, tty].tile_id == 0)
                                            world_map[ttx, tty].tile_id = -1;

                                        break;
                                    }
                                    else // enough in cell above original source
                                    {
                                        world_map[tsx, tsy].water_units -= actual_pressure_rate;
                                        world_map[ttx, tty].water_units += actual_pressure_rate;
                                        // check new source cell: assign air if needed
                                        if (world_map[tsx, tsy].water_units == 0)
                                            world_map[tsx, tsy].tile_id = 0;

                                        // assign water cell to traget
                                        if (world_map[ttx, tty].tile_id == 0)
                                            world_map[ttx, tty].tile_id = -1;

                                        break;
                                    }
                                }
                                else // target overflows
                                {
                                    if (valid_array(ttx, tty - 1) && world_map[ttx, tty - 1].tile_id <= 0) // make sure new cell can be created above current target
                                    {
                                        int remainder = actual_pressure_rate - (100 - world_map[ttx, tty].water_units); // tx and ty have been confirmed valid earlier in the code
                                        // transfer + create new water cell
                                        world_map[tsx, tsy].water_units -= actual_pressure_rate;

                                        world_map[ttx, tty].water_units = 100; // set initial target to full capacity
                                        world_map[ttx, tty - 1].tile_id = -1;
                                        world_map[ttx, tty - 1].water_units = remainder;
                                        // check new source cell: assign air if needed
                                        if (world_map[tsx, tsy].water_units == 0)
                                            world_map[tsx, tsy].tile_id = 0;
                                        if (world_map[tsx, tsy].water_units < 0)
                                        {
                                            world_map[tsx, tsy].tile_id = 0;
                                            world_map[tsx, tsy + 1].water_units += world_map[tsx, tsy].water_units; // add negative units to cell below
                                            world_map[tsx, tsy].water_units = 0; // reset
                                        }

                                        break;
                                    }
                                    else if (valid_array(ttx, tty - 1) && world_map[ttx, tty - 1].tile_id > 0) // above target is solid
                                    {
                                        if (actual_pressure_rate + world_map[ttx, tty].water_units >= 100)
                                            actual_pressure_rate = 100 - world_map[ttx, tty].water_units;

                                        world_map[ttx, tty].water_units += actual_pressure_rate; // target +
                                        world_map[tsx, tsy].water_units -= actual_pressure_rate; // source -
                                        // check new source cell: assign air if needed
                                        if (world_map[tsx, tsy].water_units == 0)
                                            world_map[tsx, tsy].tile_id = 0;

                                        break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }// END PRESSURE FROM DIFFERENT HEIGHTS
                            else if (tsy == tty) // same level
                            {
                                actual_pressure_rate = calculate_transfer_rate(tsx, tsy, ttx, tty); // recalculate pressure rate for new source/target combination

                                if (actual_pressure_rate > pressure_rate)
                                {
                                    actual_pressure_rate = pressure_rate;
                                }
                                // determine if transfer is possible
                                if (actual_pressure_rate > 0)
                                {
                                    world_map[tsx, tsy].water_units -= actual_pressure_rate;
                                    world_map[ttx, tty].water_units += actual_pressure_rate;
                                    // fix new water/air cells
                                    if (world_map[tsx, tsy].water_units == 0)
                                    {
                                        world_map[tsx, tsy].tile_id = 0;
                                    }
                                    if (world_map[ttx, tty].tile_id == 0)
                                    {
                                        world_map[ttx, tty].tile_id = -1;
                                    }
                                    break;
                                }
                                else
                                {
                                    // try to find a new source to the left and continue the loop now with a different original source
                                    if (valid_array(sx - 1, sy) && world_map[sx - 1, sy].tile_id == -1 && world_map[sx - 1, sy - 1].tile_id == -1)
                                    {
                                        source_backtracker++;
                                        offset--; // offset will increase, set it back to current in order to have the same target but different source
                                        continue;
                                    }
                                    else
                                        break; // actual rate is 0 and no available backup source
                                }
                            }
                        }
                        else // bad condition
                        {
                            if (valid_array(sx - 1, sy) && world_map[sx - 1, sy].tile_id == -1 && world_map[sx - 1, sy - 1].tile_id == -1)
                            {
                                source_backtracker++;
                                offset--; // offset will increase, set it back to current in order to have the same target but different source
                                continue;
                            }
                            else
                                break; // actual rate is 0 and no available backup source
                        }
                    }
                    else
                    {
                        if(rule_p1 && !rule_p2)
                        {
                            if (source_backtracker == 0)
                                continue; // find a better target
                            else
                            {
                                break;
                            }
                        }
                        else if(!rule_p1)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break; // if cells are not both 100
                }
            }// second offset loop
*/