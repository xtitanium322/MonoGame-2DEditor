using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System.Xml;                                                           // use xml files
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;  // xml serialization
using MyDataTypes;                                                          // data types structure for xml serialization
using System.IO;                                                            // filestream
/*
 * Generates a playable map for the hero to explore
 */
namespace EditorEngine
{
    /// <summary>
    /// Grass representing entity
    /// </summary>
    public class Grass
    {
        Vector2 cell_address;    // cell position
        long creation_time;      // millisecond of creation 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="cell">cell address</param>
        public Grass(Engine e,Vector2 cell)
        {
            cell_address = cell;
            creation_time = Engine.get_current_game_millisecond();
        }
        /// <summary>
        /// get current cell address
        /// </summary>
        /// <returns></returns>
        public Vector2 get_cell_address()
        {
            return cell_address;
        }
        /// <summary>
        /// Getr the creation time
        /// </summary>
        /// <returns>millisecond of creation</returns>
        public long get_creation_time()
        {
            return creation_time;
        }
        /// <summary>
        /// Update or set the creation time
        /// </summary>
        /// <param name="v">millisecond value</param>
        public void set_creation_time(long v)
        {
            creation_time = v;
        }
    }
    /// <summary>
    /// The world - contains tiles and props
    /// Main action arena
    /// </summary>
    public class World
    {
        private int w;                             // width/width
        private int h;                             // height 
        // water simulation variables
        private const int water_zone_width = 240;  // number of horizontal cells in a single simulation zone multiplied by all vertical cells
        private int water_zone_edge = 0;           // left most cell + water_zone_width = boundary for sim calculations
        private int water_zone_stopper = 0;        // right most cell = boundary
        private const int surface_smoothness = 16; // straightens water surface this many cells away from the currently checked source

        private const int tile_size = 20;          // size of square in pixels
        private const int GRASS_GROWTH_DELAY = 4500;
        private String world_name;                 // name of this playable world
        private bool edit_mode;                    // is the world in edit mode?
        private Vector2 map_origin;                // map origin (top left corner)
        public tile_map[,] world_map;              // contains all Tile definitions for this world
        private Stack<Vector2> updated_cells;      // contains every cell that has been updated during current frame, cells deleted after calculations
        private Color[] sky_color;                 // an array of values for world background color in relation to world time
        private Color sky;                         // current color of the sky
        public List<PointLight> world_lights;      // a list of all highlighted lights   
        public List<WaterGenerator> wsources;      // a list of all highlighted lights 
        Rectangle[] corner_src;
        public List<Tree> trees;                   // a list of trees in this world
        public List<Grass> grass_tiles;            // list of grass objects
        
        public short[] valid_tree_bases;

        // prepare various grass textures
        Texture2D single_grass;
        Texture2D grass_corner;
        Texture2D grass_corner_top;
        Texture2D grass_one;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="content">content manager object</param>
        /// <param name="name">world name</param>
        /// <param name="length">number of cells - width</param>
        /// <param name="height">number of cell height, multiplied by length - get the total number of cells</param>
        public World(Engine engine, ContentManager content, String name, int length, int height)
        {
            world_map = new tile_map[length, height]; // create an array which accepts tile_map structures
            edit_mode = false;
            w = length;
            h = height;
            world_name = name;
            map_origin = new Vector2(0, 0);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    world_map[i, j] = new tile_map(0, 0); // create air ui_elements, id=0, variant = 0
                }
            }
            // create memory for updated cells
            updated_cells = new Stack<Vector2>();
            // create World Light list
            world_lights = new List<PointLight>();
            wsources = new List<WaterGenerator>();
            grass_tiles = new List<Grass>();
            trees = new List<Tree>();
            
            sky = new Color(135, 206, 235);          // default sky color
            // sky colors (effects to be added for sun and moon transitions
            sky_color = new Color[3];
            sky_color[0] = new Color(10, 10, 10);      // midnight color 
            sky_color[1] = new Color(135, 206, 235);   // noon color
            sky_color[2] = new Color(0, 191, 250);     // day color 
            corner_src = new Rectangle[4];
            valid_tree_bases = new short[] { 1, 2, 5, 9 }; // test cell, dirt, sand, snow

            single_grass = engine.get_texture("grass_single");
            grass_corner = engine.get_texture("grass2");
            grass_corner_top = engine.get_texture("grass_corner_top");
            grass_one = engine.get_texture("grass1");
        }
        /// <summary>
        /// Load any related assets
        /// </summary>
        /// <param name="content">content manager</param>
        /// <param name="engine">engine instance</param>
        public void LoadContent(ContentManager content, Engine engine)
        {
        }
        /// <summary>
        /// Draw all the tiles and props
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="sb">spritebatch</param>
        public void draw_trees_part1(Engine engine, SpriteBatch sb)
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

            // 1. tree loop - second section - trunks and branches
            // any variant of the abstract Tree entity, e.g. GreenTree, PalmTree etc (any future trees) will be drawn using the generalized rendering sequence below
            foreach (Tree t in trees)
            {
                Vector2 base_position = (t.get_position() * tilesize - new Vector2(20, 20)) - engine.get_camera_offset(); // - 20 20 vector to compensate because should start drawing on the left top corner

                // draw trunks and branches
                // get_name_modifier() function return a unique tree name modifier (for each tree modifier a correct sprite/texture needs to be added)
                Texture2D trunk_sprite = engine.get_texture(t.get_name_modifier() + "trunk1");
                Texture2D branch_sprite = engine.get_texture(t.get_name_modifier() + "branch1");
                Texture2D branch_sprite_right = engine.get_texture(t.get_name_modifier() + "branch1");

                int growth_adjustment = 40 - (int)(40 * engine.get_percentage_of_range(0, t.get_growth_rate(), Engine.get_current_game_millisecond() - t.get_last_growth()));// if the tree is still growing - animate height differential

                List<Trunk> tr = t.get_trunks();
                // use this value to get cropping rectangle, simulates trunk narrowing toward the tree peak
                // this value will also be added to each sprite to position it further away from origin horizontally, so that everything remains centered
                int width_adjuster = tr.Count;

                for (int i = tr.Count - 1; i >= 0; i--)
                {
                    width_adjuster--;

                    //draw trunk segments                  
                    Vector2 offset = new Vector2(0, -((i + 1) * trunk_sprite.Height));
                    trunk_sprite = engine.get_texture(t.get_name_modifier() + "trunk" + tr[i].get_variant());
                    // trunk origin will be the middle of its base, so an offset is needed
                    // an offset above puts every trunk segment at correct height based on its order
                    if (i == (t.get_trunks().Count - 1)) // last trunk
                    {
                        float trunk_scale = 0.5f + 0.5f * (float)(engine.get_percentage_of_range(-1, t.get_growth_rate(), Engine.get_current_game_millisecond() - t.get_last_growth()));

                        engine.xna_draw(
                            trunk_sprite,
                            base_position + new Vector2(10 + (width_adjuster / 2), 20) + offset + new Vector2(0, growth_adjustment),
                            new Rectangle(width_adjuster, 0, trunk_sprite.Width - (width_adjuster * 2), trunk_sprite.Height),
                            t.get_tint_color(engine),
                            0f,
                            new Vector2(trunk_sprite.Width / 2, trunk_sprite.Height),
                            trunk_scale,
                            SpriteEffects.None,
                            0.1f);
                    }
                    else
                    {
                        engine.xna_draw(
                            trunk_sprite, base_position + new Vector2(10 + (width_adjuster / 2), 20) + offset,
                            new Rectangle(width_adjuster, 0, trunk_sprite.Width - (width_adjuster * 2), trunk_sprite.Height),
                            t.get_tint_color(engine), 0f, new Vector2(trunk_sprite.Width / 2, trunk_sprite.Height), 1f, SpriteEffects.None, 1f);
                    }
                    //1.2-----------------------draw branches
                    Branch left = tr[i].get_left();
                    Branch right = tr[i].get_right();
                    long since_last_trunk_growth = Engine.get_current_game_millisecond() - t.get_last_growth();

                    float scale = 1f; // simulate popping up
                    float opacity = 1f;

                    if (left != null)
                    {
                        if (i >= t.get_trunks().Count - 2) // if last 2 trunks (growth of the branches is post-poned by a full growth cycle
                        {   // scale up with a delay after branch creation
                            scale = (float)(engine.get_percentage_of_range(0, t.get_growth_rate(), Engine.get_current_game_millisecond() - t.get_growth_rate() - left.get_creation()));
                            scale = scale < 0 ? 0 : scale; // adjust for delay
                        }

                        branch_sprite = engine.get_texture(t.get_name_modifier() + (left.has_leaves() ? "l" : "") + "branch" + left.calculate_variant().ToString());
                        // calculate branch offset by: taking the branches vertical -offset value and horizontal - half of trunk width for left facing
                        // set sprite origin bottom right corner
                        engine.xna_draw(
                        branch_sprite, base_position + new Vector2(15 + width_adjuster, 20) + offset - new Vector2(trunk_sprite.Width / 2, left.get_offset()),
                        null,
                        t.get_tint_color(engine) * opacity,
                        0f, new Vector2(branch_sprite.Width, branch_sprite.Height), scale, SpriteEffects.None, 1f);
                    }

                    if (right != null)
                    {
                        if (i >= t.get_trunks().Count - 2) // if last 2 trunks
                        {
                            scale = (float)(engine.get_percentage_of_range(0, t.get_growth_rate(), Engine.get_current_game_millisecond() - t.get_growth_rate() - right.get_creation()));
                            scale = scale < 0 ? 0 : scale;
                        }

                        branch_sprite_right = engine.get_texture(t.get_name_modifier() + (right.has_leaves() ? "l" : "") + "branch" + right.calculate_variant().ToString());
                        // calculate branch offset by: taking the branches vertical -offset value and horizontal + half of trunk width for right facing
                        // set sprite origin bottom left corner and flip horizontally
                        engine.xna_draw(
                        branch_sprite_right, base_position + new Vector2(5 - width_adjuster, 20) + offset + new Vector2(trunk_sprite.Width / 2, -right.get_offset()),
                        null,
                        t.get_tint_color(engine) * opacity,
                        0f, new Vector2(0, branch_sprite_right.Height), scale, SpriteEffects.FlipHorizontally, 1f);
                    }
                }

                // 2. draw tree crown
                Texture2D crown = engine.get_texture(t.get_name_modifier() + "leaves" + t.get_crown_variant());
                Vector2 crown_position_offset = new Vector2(10, -((t.get_trunks().Count + 1) * trunk_sprite.Height));

                float crown_scale = 1f; // simulate popping up

                if (t.get_trunks().Count == t.get_max_trunks())
                {   // delay the crown by a full growth cycle for better animation
                    crown_scale = (float)(engine.get_percentage_of_range(0, t.get_growth_rate(), Engine.get_current_game_millisecond() - t.get_growth_rate() - t.get_last_growth()));
                    crown_scale = crown_scale < 0 ? 0 : crown_scale;

                    engine.xna_draw(
                    crown, base_position + new Vector2(0, 20) + crown_position_offset,
                    null,
                    t.get_tint_color(engine), 0f, new Vector2(crown.Width / 2, crown.Height / 2), crown_scale, SpriteEffects.None, 1f);
                }
            }
        }
       
        /// <summary>
        /// Draw the tiles only
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="sb">spritebatch</param>
        public void draw_map_tiles(Engine engine, SpriteBatch sb)
        {
            // 3. Tree loop = base section (drawn after trunks, to hide the growing process
            foreach (Tree t in trees)
            {
                Vector2 base_position = (t.get_position() * tilesize - new Vector2(20, 20)) - engine.get_camera_offset(); // - 20 20 vector to compensate because should start drawing on the left top corner


                // draw base last - to cover initital growth sprite
                // base sprite origin is 10 pixels from the bottom and in the middle horizontally
                Texture2D base_sprite = engine.get_texture(t.get_name_modifier() + "tree_base" + t.get_base_variant().ToString());

                engine.xna_draw(
                    base_sprite, base_position + new Vector2(10, 20), // 10,20 to compensate for cell top corner being the origin
                    null,
                    t.get_tint_color(engine), 0f, new Vector2(base_sprite.Width / 2, (base_sprite.Height - (base_sprite.Height - 40))), 1f, SpriteEffects.None, 1f);
            }

            Vector2 min_limit = min_visible_tile(engine);
            Vector2 max_limit = max_visible_tile(engine);
            Rectangle src = new Rectangle();
            Vector2 position = Vector2.Zero;
        // 4. tile loop - go through all ui_elements and render building blocks of the ground
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
         // 5. grass loop
            // draw grass

            // corners section
            foreach (Grass g in grass_tiles)
            {
                Vector2 grassposition = (g.get_cell_address() * tile_size) - new Vector2(20, 20);
                // check visibility
                if (!engine.is_within_visible_screen(grassposition - engine.get_camera_offset()))
                    continue;
                float scale = 0.25f + (float)(0.75 * (engine.get_percentage_of_range(0, GRASS_GROWTH_DELAY, Engine.get_current_game_millisecond() - g.get_creation_time())));
                int height_difference = 5 - (int)(5 * (float)(engine.get_percentage_of_range(0, GRASS_GROWTH_DELAY, Engine.get_current_game_millisecond() - GRASS_GROWTH_DELAY - g.get_creation_time()))); // adjust 1 growth period for creeping down of the corner (because grass does not cover full ground cell)
                // corners
                Vector2 checking_this = engine.neighbor_cell(g.get_cell_address(), "left", 1, 1);

                if (grass_tiles.Find(x => x.get_cell_address() == checking_this) != null) // left and 1 down
                {
                    // draw left corner
                    engine.xna_draw(
                    grass_corner,
                    grassposition - engine.get_camera_offset() + new Vector2(0, 25 - height_difference), // where + compensation for centered sprite origin
                    null, Color.White * scale, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                }

                checking_this = engine.neighbor_cell(g.get_cell_address(), "right", 1, 1);

                if (grass_tiles.Find(x => x.get_cell_address() == checking_this) != null) // left and 1 down
                {
                    // draw right corner
                    engine.xna_draw(
                    grass_corner,
                    grassposition - engine.get_camera_offset() + new Vector2(20, 25 - height_difference), // where + compensation for centered sprite origin
                    null, Color.White * scale, 0f, new Vector2(20, 0), scale, SpriteEffects.FlipHorizontally, 1f);
                }
            }
            // draw only the single cells now
            foreach (Grass g in grass_tiles)
            {
                Vector2 grassposition = (g.get_cell_address() * tile_size) - new Vector2(20, 20);
                // check visibility
                if (!engine.is_within_visible_screen(grassposition - engine.get_camera_offset()))
                    continue;

                float scale = 0.25f + (float)(0.75 * (engine.get_percentage_of_range(0, GRASS_GROWTH_DELAY, Engine.get_current_game_millisecond() - g.get_creation_time())));
                // check if it's the left or - right most cell
                if (grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "left", 1)) == null
                    && grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "right", 1)) == null
                    )
                {// no grass tiles on either side
                    engine.xna_draw(
                    single_grass,
                    grassposition - engine.get_camera_offset() + new Vector2(10, 20), // where + compensation for centered sprite origin
                    null, Color.White * scale, 0f, new Vector2(10, 20), 1f, SpriteEffects.None, 1f);
                }
            }
            //draw corner grass
            foreach (Grass g in grass_tiles)
            {
                Vector2 grassposition = (g.get_cell_address() * tile_size) - new Vector2(20, 20);
                // check visibility
                if (!engine.is_within_visible_screen(grassposition-engine.get_camera_offset()))
                    continue;


                float scale = 0.25f + (float)(0.75 * (engine.get_percentage_of_range(0, GRASS_GROWTH_DELAY, Engine.get_current_game_millisecond() - g.get_creation_time())));
                
                if (grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "left", 1)) == null
                    && grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "right", 1)) != null)
                {// no grass on the left
                    engine.xna_draw(
                            grass_corner_top,
                            grassposition - engine.get_camera_offset() + new Vector2(10, 20), // where + compensation for centered sprite origin
                            null, Color.White * scale, 0f, new Vector2(10, 20), 1f, SpriteEffects.None, 1f);
                }
                else if (grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "right", 1)) == null
                        && grass_tiles.Find(x => x.get_cell_address() == engine.neighbor_cell(g.get_cell_address(), "left", 1)) != null)
                {// no grass on the right
                    engine.xna_draw(
                    grass_corner_top,
                    grassposition - engine.get_camera_offset() + new Vector2(10, 20), // where + compensation for centered sprite origin
                    null, Color.White * scale, 0f, new Vector2(10, 20), 1f, SpriteEffects.FlipHorizontally, 1f);
                }
                else // both exist
                {
                    engine.xna_draw(
                    grass_one,
                    grassposition - engine.get_camera_offset() + new Vector2(10, 20), // where + compensation for centered sprite origin
                    null, Color.White * scale, 0f, new Vector2(10, 20), 1f, SpriteEffects.None, 1f);
                }
            }                
        // 6. water tile loop
            for (int i = (int)min_limit.X - 1; i < (int)max_limit.X; i++)
            {
                for (int j = (int)min_limit.Y - 1; j < (int)max_limit.Y; j++)
                {
                    if (world_map[i, j].tile_id != -1) // only draw water ui_elements
                        continue;

                    tile_map current;
                    tile_map above_current;
                    tile_map below_current;              

                    float water_vol = world_map[i, j].until_empty(); // amount of air
                    int air_pixels = tile_size - (int)((water_vol / 100f) * tile_size);

                    src.X = 0; src.Y = air_pixels; src.Width = tile_size; src.Height = tile_size - air_pixels;
                    position.X = i * tile_size;
                    position.Y = map_origin.Y + j * tile_size + air_pixels;
                    // draw this cell
                    if (valid_array(i, j - 1) && valid_array(i, j))
                    {
                        current = world_map[i, j];
                        above_current = world_map[i, j - 1];

                        if (current.water_units == 100)
                        {
                            src.Y = 0; src.Height = tile_size;
                            position.Y = map_origin.Y + j * tile_size;

                            engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                             Engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                        else // not full
                        {
                            if (current.water_units > 0 && above_current.tile_id == 0) // not full and nothing above
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 Engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            else if ((current.water_units > 0 && above_current.water_units > 0 && above_current.tile_id == -1) || current.flow == true) // not full / something above
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 Engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);

                                src.Y = 0; src.Height = air_pixels;
                                position.Y = map_origin.Y + j * tile_size;
                                // flow
                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                  Engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.25f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            else
                            {
                                src.Y = air_pixels; src.Height = tile_size - air_pixels;
                                position.Y = map_origin.Y + j * tile_size + air_pixels;

                                engine.xna_draw(Engine.pixel, position - engine.get_camera_offset(), src, Color.Lerp(Color.Aquamarine, Color.Aquamarine,
                                                 Engine.fade_sine_wave_smooth(3500, 0.0f, 1.0f)) * 0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Update world: Tile connections and water loop
        /// </summary>
        /// <param name="c">world clock object</param>
        /// <param name="engine">engine instance</param>
        public void update_world(WorldClock c, Engine engine)
        {
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
            
            // particle emitter check
            // engine.get_particle_engine().update(engine);

            // clear update get_cell_address matrix
            if (updated_cells.Count > 0)
                updated_cells.Clear(); // delete cells from stack
            // water generation 
            foreach (WaterGenerator w in wsources)
            {
                int x = (int)w.get_cell_address().X;
                int y = (int)w.get_cell_address().Y;

                // check tile for air
                if (world_map[x, y].tile_id != -1)
                {
                    world_map[x, y].tile_id = -1;
                }

                // add water based on generator intensity
                world_map[x, y].water_units += w.get_intensity();

                // check validity
                if (world_map[x, y].water_units > 100)
                    world_map[x, y].water_units = 100;
            }
            // water update  
            water_simulation(engine);


            // grass spreading       
            List<Grass> temp = new List<Grass>();

            for (int i = grass_tiles.Count - 1; i >= 0; i-- )
            {
                // check if grass has lost the ground under itself - if yes - delete (need to read list from the back for this to work, otherwise the 'System.InvalidOperationException' will occur = change to a List while List is being read)
                if (valid_cell(engine.neighbor_cell(grass_tiles[i].get_cell_address(), "bottom", 1)))
                {
                    Vector2 checking_this = engine.neighbor_cell(grass_tiles[i].get_cell_address(), "bottom", 1);

                    if (get_tile_id(checking_this) == 0)// if ground has become air - remove the grass overlay
                    {
                        grass_tiles.RemoveAt(i);
                    }
                }             
            }

            // Spread sequence in its own loop, in case some cells have been deleted
            foreach (Grass g in grass_tiles)
            {               
                if (Engine.get_current_game_millisecond() - g.get_creation_time() > GRASS_GROWTH_DELAY)
                {
                    // spread to the right
                    if (valid_cell(engine.neighbor_cell(g.get_cell_address(), "right", 1)))
                    {
                        Vector2 checking_this = engine.neighbor_cell(g.get_cell_address(), "right", 1);
                        if (
                            grass_tiles.Find(x => x.get_cell_address() == checking_this) == null
                            && get_tile_id(checking_this + new Vector2(0, 1)) == 2     // right cell ground is dirt
                            && get_tile_id(checking_this) == 0                         // future grass cell is air
                            )
                        {
                            temp.Add(new Grass(engine, engine.neighbor_cell(g.get_cell_address(), "right", 1)));
                        }
                    }
                    // spread to the left
                    if (valid_cell(engine.neighbor_cell(g.get_cell_address(), "left", 1)))
                    {
                        Vector2 checking_this = engine.neighbor_cell(g.get_cell_address(), "left", 1);
                        if (
                            grass_tiles.Find(x => x.get_cell_address() == checking_this) == null
                            && get_tile_id(checking_this + new Vector2(0, 1)) == 2     // left cell ground is dirt
                            && get_tile_id(checking_this) == 0                         // future grass cell is air
                            )
                        {
                            temp.Add(new Grass(engine, engine.neighbor_cell(g.get_cell_address(), "left", 1)));
                        }
                    }
                }
            }
            // combine lists
            grass_tiles.AddRange(temp);
        }

        /// <summary>
        /// Simulates water flow
        /// </summary>
        /// <param name="engine"> engine object </param>
        public void water_simulation(Engine engine)
        {
            // zone adjustment (to save CPU processing power)
            water_zone_edge += water_zone_width;

            // reset to 1st cell if edge is beyond last cell in the world
            if (water_zone_edge >= w)
                water_zone_edge = 0;

            // adkust right edge
            water_zone_stopper = water_zone_edge + water_zone_width; // where the last cell is
            //adjust final horizontal cell
            if (water_zone_stopper > w)
                water_zone_stopper = w;
            // simulation
            int rate = 16; // vertical rate

            for (int y = h; y >= 0; y--) // bottom --> top
            {
                for (int x = water_zone_edge; x < water_zone_stopper; x++) // left --> right (adjusted for simulation zones)
                {
                    // --------------------------------------------
                    // VERTICAL flow
                    // --------------------------------------------
                    if (valid_array(x, y + 1))
                    {
                        if (world_map[x, y].tile_id > 0)
                            continue;
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
                    // --------------------------------------------                    
                    // right transfer 
                    int preferred_rate = 8;
                    int actual_rate = 0; // calculated for each pair of cells
                    int sx = 0, sy = 0; // source coordinates
                    int tx = 0, ty = 0; // target coordinates

                    if (valid_array(x, y) && valid_array(x + 1, y)) // check initial current and right cells to be within borders
                    {
                        //      for RIGHT transfers - set of rules:
                        // 0. if right cell has the same or smaller number of water units and it's not 100 - break the loop without transfer
                        // 1. calculate actual rate - if within 1-preferred rate = ok, if more - set to preferred rate, if 0 - check rule 1A
                        // 1A. check cell above current source - if it's water make it new source but add 100 to water_units for calculation purposes. then recalculate actual rate and transfer
                        //      if rule 1A was used - break out of the for loop after transfer
                        // 2. if there is no cell above - continue offset loop until good target cell is found or termination condition met (solid cell)
                        //      note: if air cell is found - it counts as target with 0 units only if it has 100unit cell or solid below it.
                        for (int offset = 1; offset < surface_smoothness; offset++)
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
                                            }

                                            if (world_map[sx, sy].water_units < 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy + 1].water_units += world_map[sx, sy].water_units; // add negative units to cell below
                                                world_map[sx, sy].water_units = 0; // reset
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

                                    // rules
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
                                    else
                                    {
                                        if(ty+1 == h) //world edge treated as water
                                        {
                                            rule1 = true;
                                        }
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
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }// end offset loop
                    }// right horizontal flow end
                    // --------------------------------------------
                    // left transfer
                    if (valid_array(x, y) && valid_array(x - 1, y)) // check initial current and right cells to be within borders
                    {
                        //      for RIGHT transfers - set of rules:
                        // 0. if right cell has the same or smaller number of water units and it's not 100 - break the loop without transfer
                        // 1. calculate actual rate - if within 1-preferred rate = ok, if more - set to preferred rate, if 0 - check rule 1A
                        // 1A. check cell above current source - if it's water make it new source but add 100 to water_units for calculation purposes. then recalculate actual rate and transfer
                        //      if rule 1A was used - break out of the for loop after transfer
                        // 2. if there is no cell above - continue offset loop until good target cell is found or termination condition met (solid cell)
                        //      note: if air cell is found - it counts as target with 0 units only if it has 100unit cell or solid below it.
                        for (int offset = 1; offset < surface_smoothness; offset++)
                        {
                            // variables setup
                            sx = x; sy = y;          // current source
                            tx = x - offset; ty = y; // current target
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
                                            }

                                            if (world_map[sx, sy].water_units < 0)
                                            {
                                                world_map[sx, sy].tile_id = 0;
                                                world_map[sx, sy + 1].water_units += world_map[sx, sy].water_units; // add negative units to cell below
                                                world_map[sx, sy].water_units = 0; // reset
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
                                    bool rule4 = false; // 4. left ledge = source with solid below, target is air

                                    if (valid_array(tx, ty + 1)
                                        && world_map[tx, ty].tile_id == -1
                                        && world_map[tx, ty + 1].tile_id != 0
                                        )
                                        rule1 = true;
                                    else if (world_map[tx, ty].tile_id == 0 && valid_array(tx, ty + 1))
                                    {
                                        if (world_map[tx, ty + 1].tile_id > 0) //bottom cell is solid
                                            rule2 = true;
                                        else if (world_map[tx, ty + 1].tile_id == -1 && world_map[tx, ty + 1].water_units > 90) // overflow for almost filled cells
                                            rule3 = true;
                                    }
                                    else
                                    {
                                        if (ty + 1 == h) //world edge treated as water
                                        {
                                            rule1 = true;
                                        }
                                    }
                                    //ledge (rule4 changed to properly account for 1 cell before target - ledge.
                                    if (valid_array(tx + 1, ty + 1) // one before ledge
                                        && world_map[tx + 1, ty + 1].tile_id > 0 // one before ledgte must be a solid

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
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }// end offset loop
                    }// horizontal flow end

                    // ===========================
                    // evaporation sequence
                    if(world_map[sx, sy].tile_id == -1 && world_map[sx,sy].water_units == 1)
                    {
                        world_map[sx, sy].tile_id = 0;
                        world_map[sx, sy].water_units = 0;
                    }
                }//main for loop
            }// main for loop     
        }// function end

        /// <summary>
        /// Water untis transferred between cells
        /// </summary>
        /// <param name="sx">start cell x </param>
        /// <param name="sy">start cell y</param>
        /// <param name="tx">target cell x</param>
        /// <param name="ty">target cell y</param>
        /// <param name="adjustment">adjusted rate</param>
        /// <returns>the actual transfer value</returns>
        public int calculate_transfer_rate(int sx, int sy, int tx, int ty, int adjustment = 0)
        {
            return ((world_map[sx, sy].water_units + adjustment) - world_map[tx, ty].water_units) / 2;
        }
        /// <summary>
        /// delete all water cells
        /// </summary>
        public void reset_water()
        {
            for (int y = 0; y <= h; y++)
            {
                for (int x = 0; x <= w; x++)
                {
                    // prime variables for next run
                    if (valid_array(x, y) && world_map[x,y].tile_id == -1)
                    {
                        world_map[x, y].water_units = 0;
                        world_map[x, y].tile_id = 0;
                    }
                }
            }
        }
        /// <summary>
        /// Removes all point lights
        /// </summary>
        public void destroy_lights()
        {
            world_lights.Clear();
        }
        /// <summary>
        /// delete all trees
        /// </summary>
        public void destroy_trees()
        {
            trees.Clear();
        }
        /// <summary>
        /// delete all grass
        /// </summary>
        public void destroy_grass()
        {
            grass_tiles.Clear();
        }
        /// <summary>
        /// Removes all water generators
        /// </summary>
        public void destroy_water_generators()
        {
            wsources.Clear();
        }
        /// <summary>
        /// Create a tree
        /// </summary>
        /// <param name="m">mouse</param>
        /// <param name="engine">engine</param>
        public void generate_tree_base(MouseState m, Engine engine)
        {
            Vector2 cell = get_current_hovered_cell(m, engine);

            if (engine.get_editor().preview_tree((int)cell.X, (int)cell.Y, engine, new short[]{2,9}, true) // green tree grows on test cell, dirt and snow
                && trees.Find(x => engine.are_vectors_equal(x.get_position(), cell)) == null // no other tree exists in this cell
            )
            {
                trees.Add(new GreenTree(engine, cell, Engine.generate_int_range(1250, 6500))); // create trees with variable growthrates

                Vector2 ground_cell = cell + new Vector2(0, 1);
                // no grass on snow
                if (get_tile_id(ground_cell) != 9)
                    grass_tiles.Add(new Grass(engine, cell));
                // check for extra grass possibilities
                // check for dirt and air above it
                for (int i = 1; i <= 2; i++)
                {
                    if (get_tile_id(engine.neighbor_cell(ground_cell, "right", i)) == 2 && get_tile_id(engine.neighbor_cell(ground_cell, "right", i, -1)) == 0)
                    {

                        if (!grass_tiles.Contains(new Grass(engine, engine.neighbor_cell(cell, "right", 1))))
                            grass_tiles.Add(new Grass(engine,engine.neighbor_cell(cell, "right", i)));
                    }
                    else
                    {
                        break;
                    }
                }
                for (int i = 1; i <= 2; i++)
                {
                    if (get_tile_id(engine.neighbor_cell(ground_cell, "left", i)) == 2 && get_tile_id(engine.neighbor_cell(ground_cell, "left", i, -1)) == 0)
                    {
                        if (!grass_tiles.Contains(new Grass(engine, engine.neighbor_cell( cell, "left", 1))))
                            grass_tiles.Add(new Grass(engine, engine.neighbor_cell(cell, "left", i)));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (engine.get_editor().preview_tree((int)cell.X, (int)cell.Y, engine, new short[] { 5 }, true) // plam grows on test cells and sand
                && trees.Find(x => engine.are_vectors_equal(x.get_position(), cell)) == null // no other tree exists in this cell
            )
            {
                trees.Add(new PalmTree(engine, cell, Engine.generate_int_range(1250, 6500))); // create trees with variable growthrates

                // 50% chance to generate some grass under a palm tree in sand
                if (Engine.generate_int_range(0, 100) > 50)
                {
                    grass_tiles.Add(new Grass(engine, cell));

                    Vector2 ground_cell = cell + new Vector2(0, 1);

                    // check for extra grass possibilities
                    // check for sand and air above it - won't spread in auto update, but original tree base will exist
                    for (int i = 1; i <= 2; i++)
                    {
                        if (get_tile_id(engine.neighbor_cell(ground_cell, "right", i)) == 5 && get_tile_id(engine.neighbor_cell(ground_cell, "right", i, -1)) == 0)
                        {

                            if (!grass_tiles.Contains(new Grass(engine, engine.neighbor_cell(cell, "right", 1))))
                                grass_tiles.Add(new Grass(engine, engine.neighbor_cell(cell, "right", i)));
                        }
                        else
                        {
                            break;
                        }
                    }
                    for (int i = 1; i <= 2; i++)
                    {
                        if (get_tile_id(engine.neighbor_cell(ground_cell, "left", i)) == 5 && get_tile_id(engine.neighbor_cell(ground_cell, "left", i, -1)) == 0)
                        {
                            if (!grass_tiles.Contains(new Grass(engine, engine.neighbor_cell(cell, "left", 1))))
                                grass_tiles.Add(new Grass(engine, engine.neighbor_cell(cell, "left", i)));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else if (engine.get_editor().preview_tree((int)cell.X, (int)cell.Y, engine, new short[] { 1 }, true) // any tree on test cell
                && trees.Find(x => engine.are_vectors_equal(x.get_position(), cell)) == null // no other tree exists in this cell
            )
            {
                if (Engine.generate_int_range(0, 100) > 50)
                {
                    trees.Add(new GreenTree(engine, cell, Engine.generate_int_range(1250, 6500)));
                }
                else
                {
                    trees.Add(new PalmTree(engine, cell, Engine.generate_int_range(1250, 6500))); 
                }
            }
        }
        /// <summary>
        /// generate a water source (cell that produces water)
        /// </summary>
        /// <param name="m">mouse state - for positioning</param>
        /// <param name="engine">engine object</param>
        public void generate_water_generator(MouseState m, Engine engine)
        {
            Vector2 cell = get_current_hovered_cell(m, engine);

            // add a water generator if there isn't one already and if cell is air
            if (!is_watergen_object_in_cell(cell) 
                /*&& valid_cell((int)cell.X - 1, (int)cell.Y - 1) */
                && world_map[(int)cell.X - 1, (int)cell.Y - 1].tile_id == 0)
            {
                wsources.Add(new WaterGenerator(cell - Vector2.One, get_tile_center(cell), Engine.generate_int_range(20, 80)));
            }
        }
        /// <summary>
        /// generate a light, in player-specified cell. (unless it is a default light)
        /// player/map editor input
        /// generated by left click through editor
        /// </summary>
        /// <param name="color">light color</param>
        /// <param name="m">mouse state for position</param>
        /// <param name="engine">engine instance</param>
        /// <param name="radius">light circle of effect radius</param>
        /// <param name="intensity">light brightness</param>
        public void generate_light_source(Color color, MouseState m, Engine engine, int radius, float intensity)
        {
            Vector2 cell = get_current_hovered_cell(m, engine, false); // false means - do not keep within world bounds
            if (!is_light_object_in_cell(cell))
            {
                world_lights.Add(new PointLight(engine, cell, color, get_tile_center(cell), radius, intensity));
            }
        }
        /// <summary>
        /// Generate light source given cell coordinates
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <param name="color">light color</param>
        /// <param name="m">mouse state for position</param>
        /// <param name="engine">engine instance</param>
        /// <param name="radius">light circle of effect radius</param>
        /// <param name="intensity">light brightness</param>
        public void generate_light_source(Vector2 cell, Color color, MouseState m, Engine engine, int radius, float intensity)
        {
            if (!is_light_object_in_cell(cell))
            {
                world_lights.Add(new PointLight(engine, cell, color, get_tile_center(cell), radius, intensity));
                // create a light sphere
                //world_lights.Last().create_light_sphere(radius, color, intensity);
            }
        }
        /// <summary>
        /// Automatic light generator
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="color">light color</param>
        /// <param name="cell">cell address</param>
        /// <param name="radius">light circle of effect radius</param>
        /// <param name="intensity">light brightness</param>
        public void add_light_source(Engine engine, Color color, Vector2 cell, int radius, float intensity)
        {
            world_lights.Add(new PointLight(engine, cell, color, get_tile_center(cell), radius, intensity));
            // create a light sphere
            //world_lights.Last().create_light_sphere(radius, color, intensity);
        }

        /// <summary>
        /// detect a light in the cell
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>true or false</returns>
        public bool is_light_object_in_cell(Vector2 cell)
        {
            //adjustments

            foreach (PointLight pl in world_lights)
            {
                if (pl.cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// detect a water generator in the cell
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>true or false</returns>
        public bool is_watergen_object_in_cell(Vector2 cell)
        {
            foreach (WaterGenerator w in wsources)
            {
                if (w.get_cell_address() == cell)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// World width in cells
        /// </summary>
        public int width
        {
            get { return w; }
        }
        // World height in cells
        public int height
        {
            get { return h; }
        }
        /// <summary>
        /// current tilesize
        /// </summary>
        public int tilesize
        {
            get { return tile_size; }
        }
        /// <summary>
        /// World name
        /// </summary>
        public String worldname
        {
            get { return world_name; }
        }

        /// <summary>
        /// does the Tile exist in the cell
        /// </summary>
        /// <param name="x">cell address pos x</param>
        /// <param name="y">cell address pos y</param>
        /// <returns>true or false</returns>
        public bool tile_exists(int x, int y) // cell coordinates
        {
            // contain in boundaries
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal width/height of map due to arrays starting at 0
                return false;
            // ui_elements exists
            if (!world_map[x - 1, y - 1].is_air())
                return true;
            else
                return false;
        }
        /// <summary>
        /// Does tile exist in this cell
        /// </summary>
        /// <param name="x">cell address pos x</param>
        /// <param name="y">cell address pos y</param>
        /// <returns>true or false</returns>
        public bool tile_doesnt_exist(int x, int y) // cell coordinates
        {
            // contain in boundaries
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal width/height of map due to arrays starting at 0
                return true;
            // ui_elements exists
            if (world_map[x - 1, y - 1].is_air())
                return true;
            else
                return false;
        }
        /// <summary>
        /// Does tile exist in this cell
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>true or false</returns>
        public bool tile_exists(Vector2 cell) // cell coordinates
        {
            // contain in boundaries
            if (cell.X <= 0 || cell.Y <= 0 || cell.X > w || cell.Y > h) //coordinates can't be 0 or equal width/height of map due to arrays starting at 0
                return false;
            // ui_elements exists
            if (!world_map[(int)cell.X - 1, (int)cell.Y - 1].is_air())
                return true;
            else
                return false;
        }
        /// <summary>
        /// Toggle the edit mode on or off
        /// </summary>
        public void toggle_edit_mode()
        {
            edit_mode = !edit_mode;
            
        }
        /// <summary>
        /// Get the editor mdoe status
        /// </summary>
        /// <returns>true or false</returns>
        public bool in_edit_mode()
        {
            return edit_mode;
        }
        /// <summary>
        /// add a Tile to the map
        /// </summary>
        /// <param name="id">tile id</param>
        /// <param name="x">cell address x</param>
        /// <param name="y">cell address y</param>
        /// <param name="engine">engine instance</param>
        /// <param name="vol">water volume</param>
        /// <returns>true or false</returns>
        public bool generate_tile(short id, int x, int y, Engine engine, int vol = 0)
        {
            if (this.tile_doesnt_exist(x, y) || engine.get_editor().cell_overwrite_mode())
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
        /// <summary>
        /// Generate tiles in a matrix
        /// </summary>
        /// <param name="list">list of addresses</param>
        /// <param name="engine">engine instance</param>
        /// <param name="tile_id">tile id to create</param>
        /// <param name="vol">water volume</param>
        public void generate_matrix(List<Vector2> list, Engine engine, short tile_id, int vol = 0)
        {
            foreach (Vector2 cell in list)
            {
                generate_tile(tile_id, (int)cell.X, (int)cell.Y, engine, vol);
                updated_cells.Push(new Vector2(cell.X, cell.Y));
            }
        }
        /// <summary>
        /// Generate tiles in a hollow matrix
        /// </summary>
        /// <param name="list">list of addresses</param>
        /// <param name="engine">engine instance</param>
        /// <param name="tile_id">tile id to create</param>
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
        /// <summary>
        /// Delete tiles in a hollow matrix
        /// </summary>
        /// <param name="list">list of addresses</param>
        /// <param name="engine">engine instance</param>
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
        /// <summary>
        /// Remove tiles in a matrix
        /// </summary>
        /// <param name="list">list of addresses</param>
        /// <param name="engine">engine instance</param>
        public void erase_matrix(List<Vector2> list, Engine engine)
        {
            foreach (Vector2 cell in list)
            {
                erase_tile((int)cell.X, (int)cell.Y, engine);
                updated_cells.Push(new Vector2(cell.X, cell.Y));
            }
        }
        /// <summary>
        /// Preview tile placement
        /// </summary>
        /// <param name="id">tile id </param>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        /// <param name="radius">brush size 1-N </param>
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
        /// <summary>
        /// Preview tile given current edit mode
        /// </summary>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        /// <param name="current">current edit mode</param>
        /// <returns></returns>
        public bool preview(int x, int y, Engine engine, modes current)
        {
            if (this.tile_doesnt_exist(x, y) || engine.get_editor().cell_overwrite_mode() || current == modes.delete)
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
        /// <summary>
        /// remove Tile from map 
        /// </summary>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        /// <returns>true for success</returns>
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
        /// <summary>
        /// Udpate tile type
        /// </summary>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        public void update_tile_type(int x, int y, Engine engine)
        {
            if (valid_cell(engine) && this.tile_exists(x, y))
            {
                world_map[x - 1, y - 1].tile_id = get_current_edit_tile(engine); // update tile type here
                world_map[x - 1, y - 1].water_units = 0;
                updated_cells.Push(new Vector2(x, y)); // push updated cell on to updates stack
            }
        }
        /// <summary>
        /// Erase the tiles
        /// </summary>
        /// <param name="x">cell position x </param>
        /// <param name="y">cell position y</param>
        /// <param name="engine">engine helper</param>
        /// <param name="radius">brush radius</param>
        public void erase_tile(int x, int y, Engine engine, int radius)
        {
            Vector2 cell = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(),engine); // finds the current hovered cell address
            // check for tree
            if(trees.Find(t => engine.are_vectors_equal(t.get_position(), cell)) != null)
            {
                trees.Remove(trees.Find(t => engine.are_vectors_equal(t.get_position(), cell)));
            }
            // tiles only
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

                        // do not delete a tree base cell
                        if (trees.Find(t => engine.are_vectors_equal(t.get_position(), new Vector2(index_x+1, index_y))) == null)
                        {
                            // delete cell
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
        }
       
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
        /// <summary>
        /// Draws world user interface contexts to a render surface without applying any masking effects
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="sb">spritebatch</param>
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
        /// <summary>
        /// Draws props statistics. Currently unnecessary.
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="sb">spritebatch</param>
        public void draw_world_prop_info_layer(Engine engine, SpriteBatch sb)
        {
            if (edit_mode)
            {              
                // draw cloud statistics (~4 -5 ms)
               /*foreach (Cloud c in clouds)
               {
                    Texture2D cloud_sprite = engine.get_texture("cloud" + c.get_cloud_variant());
                    // check rain for edit mode statistics
                    /*if (in_edit_mode())
                    {
                        // rain capacity
                        if (c.is_a_rain_cloud())
                            engine.xna_draw_text_background("rain capacity: " + c.get_rain_capacity().ToString(),
                            c.get_position() - engine.get_camera_offset() + new Vector2(-cloud_sprite.Width / 2, -20),
                            Vector2.Zero, Color.SkyBlue, engine.get_UI_font(), 4);
                        else
                            engine.xna_draw_text_background("cloud not in rain mode",
                            c.get_position() - engine.get_camera_offset() + new Vector2(-cloud_sprite.Width / 2, -20),
                            Vector2.Zero, Color.White, engine.get_UI_font(), 4);

                        // other stats
                        engine.xna_draw_text_background(c.statistics(engine),
                            c.get_position() - engine.get_camera_offset() + new Vector2(-cloud_sprite.Width / 2, -40),
                            Vector2.Zero, (c.get_time_until_fadeout() <= 5) ? Color.IndianRed : Color.LawnGreen, engine.get_UI_font(), 4);

                        // in edit mode also display the emitter indicators
                        Texture2D emitter_indicator = engine.get_texture("emitter_indicator");

                        foreach(Emitter em in c.get_particle_emitters())
                        {                         
                            Vector2 pos = em.get_position() - engine.get_camera_offset();
                            engine.xna_draw(emitter_indicator, pos - new Vector2(emitter_indicator.Width / 2, emitter_indicator.Height / 2), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                        }
                    }*/
               //}
            }
        }

        /// <summary>
        /// execute editor element_command
        /// </summary>
        /// <param name="c">command type</param>
        /// <param name="engine">engine instance</param>
        /// <param name="game">game object</param>
        public void execute_command(command c, Engine engine, Game1 game)
        {
            // execute GUI editor_command
            engine.get_editor().editor_command(engine, c);
        }
        /// <summary>
        /// Get current tile type
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <returns>tile id</returns>
        public short get_current_edit_tile(Engine engine)
        {
            return engine.get_editor().get_current_editor_cell();
        }

        /// <summary>
        /// determine if the cell exists on the map, in other words - falls withn world bounds (mouse hover)
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <returns>true or false</returns>
        public bool valid_cell(Engine engine)
        {
            Vector2 hover_cell = this.get_current_hovered_cell(engine.get_current_mouse_state(), engine); // get_current_hovered_cell calculates cell numbers, not coordinates

            if (hover_cell.X > 0 && hover_cell.X <= width && hover_cell.Y > 0 && hover_cell.Y <= height)
                return true;

            return false;
        }
        /// <summary>
        /// Is this cell address valid in relation to the world map array
        /// </summary>
        /// <param name="x">cell address x</param>
        /// <param name="y">cell address y</param>
        /// <returns>true or false</returns>
        public bool valid_cell(int x, int y) //(coordinates)
        {
            if (x <= 0 || y <= 0 || x > w || y > h) //coordinates can't be 0 or equal width/height of map due to arrays starting at 0
                return false;
            else
                return true;
        }
        /// <summary>
        /// Is this array index valid in relation to the world map array
        /// </summary>
        /// <param name="x">array address x</param>
        /// <param name="y">array address y</param>
        /// <returns>true or false</returns>
        public bool valid_array(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
                return false;
            else
                return true;
        }
        /// <summary>
        /// Is this a valid cell given the vector address
        /// </summary>
        /// <param name="v">vector address</param>
        /// <returns>true or false</returns>
        public bool valid_cell(Vector2 v)
        {
            if (v.X <= 0 || v.Y <= 0 || v.X > w || v.Y > h) //coordinates can't be 0 or equal width/height of map due to arrays starting at 0
                return false;
            else
                return true;
        }
        /// <summary>
        /// calculate tile center coordinates in the world - without adjusting for camera
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>vector - cell address for centering</returns>
        public Vector2 get_tile_center(Vector2 cell)
        {
            int x = ((int)cell.X * tile_size) - (tile_size / 2);
            int y = ((int)cell.Y * tile_size) - (tile_size / 2);

            return new Vector2(x, y);
        }
        /// <summary>
        /// Where does the cell origin exist in the world (pixel address)
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>pixel address</returns>
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
        /// <returns>rectangle value</returns>
        public Rectangle get_cell_rectangle_on_screen(Engine engine, Vector2 cell)
        {
            Rectangle temp = new Rectangle();
            temp.X = ((int)cell.X * tile_size) - tile_size - (int)engine.get_camera_offset().X;
            temp.Y = ((int)cell.Y * tile_size) - tile_size - (int)engine.get_camera_offset().Y;
            temp.Width = temp.Height = tile_size;

            return temp;
        }
        /// <summary>
        /// determine if the Tile is at least partly visible on-screen
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <param name="engine">engine instance</param>
        /// <returns></returns>
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
        /// <summary>
        /// determine the furthest Tile at least partly visible on-screen
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <returns>cell address</returns>
        public Vector2 max_visible_tile(Engine engine)
        {
            // camera origin 
            int minimal_visible_cell_x = ((int)engine.get_camera_offset().X / (int)tile_size);
            int minimal_visible_cell_y = ((int)engine.get_camera_offset().Y / (int)tile_size);
            // viewport dimensions
            int max_visible_cell_x = minimal_visible_cell_x + (engine.get_viewport().Width / tile_size);
            int max_visible_cell_y = minimal_visible_cell_y + (engine.get_viewport().Height / tile_size);

            // make sure partial cells don't get cut off
            max_visible_cell_x += 1;
            max_visible_cell_y += 1;

            // check boundaries
            // x
            if (minimal_visible_cell_x < 1)
                minimal_visible_cell_x = 1;
            else if (minimal_visible_cell_x > w)
                minimal_visible_cell_x = w;

            if (max_visible_cell_x < 1)
                max_visible_cell_x = 1;
            else if (max_visible_cell_x > w)
                max_visible_cell_x = w;

            // y
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
        /// <summary>
        /// determine the closest Tile at least partly visible on-screen
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <returns>cell address</returns>
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
        /// <summary>
        /// Get the sky color
        /// </summary>
        /// <returns>sky color value</returns>
        public Color get_sky_color()
        {
            return sky;
        }

        /// <summary>
        /// returns number of filled tiles for the progress bar
        /// </summary>
        /// <returns>number of filled cells</returns>
        public int get_total_cells_filled()
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
        /// <summary>
        /// Get the world size
        /// </summary>
        /// <returns>total cells in the world</returns>
        public int get_world_size()
        {
            return w * h;
        }
        /// <summary>
        /// Percentage of world fill
        /// </summary>
        /// <returns>float value 0-1</returns>
        public float get_percent_filled()
        {
            return (float)get_total_cells_filled() / (float)(w * h);
        }
        /// <summary>
        /// draw a box surrounding the world boundaries
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="sb">spritebatch</param>
        public void draw_world_bounds(Engine engine, SpriteBatch sb)
        {

            //draw lines (rectangle (0,0,width of line, height of line)
            if (edit_mode && !engine.get_editor().gui_move_mode)
            {
                engine.xna_draw(Engine.pixel, // top
                    map_origin - engine.get_camera_offset(), // origin of the line
                    new Rectangle(0, 0, tile_size * width, 1),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // bottom
                    map_origin - engine.get_camera_offset() + new Vector2(0, tile_size * height), // origin of the line
                    new Rectangle(0, 0, tile_size * width, 1),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // left
                    map_origin - engine.get_camera_offset(), // origin of the line
                    new Rectangle(0, 0, 1, tile_size * height),
                    engine.adjusted_color(engine.get_grid_color(), 0.85f), 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                engine.xna_draw(Engine.pixel, // right
                    map_origin - engine.get_camera_offset() + new Vector2(tile_size * width, 0), // origin of the line
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
        /// <summary>
        /// detects which cell was clicked/hovered in (returns cell numbers)
        /// </summary>
        /// <param name="state">mouse for positioning</param>
        /// <param name="engine">engine instance</param>
        /// <param name="keep_within_world_bounds">check if the hovered cell is within the world bounds, if no - adjust to the closest border - only if the flag is set to true</param>
        /// <returns></returns>
        public Vector2 get_current_hovered_cell(MouseState state, Engine engine, bool keep_within_world_bounds = true)
        {
            int horizontal_cell = 0;
            int vertical_cell = 0;
            // get x and y relative to the map
            int x = state.X + (int)engine.get_camera_offset().X;
            int y = (int)map_origin.Y + (int)engine.get_camera_offset().Y + state.Y;

            horizontal_cell = x / tile_size + 1;
            vertical_cell = y / tile_size + 1;

            // adjustments
            if (!keep_within_world_bounds)
            {
                // based on the quadrant
                if (x < 0 && y < 0)
                {
                    horizontal_cell--;
                    vertical_cell--;
                }
                else if(x < 0 && y > 0)
                {
                    horizontal_cell--;
                }
                else if(x > 0 && y < 0)
                {
                    vertical_cell--;
                }
            }
            else // keep boundaries
            {
                if (horizontal_cell <= 0)
                    horizontal_cell = 1;
                if (horizontal_cell > w)
                    horizontal_cell = w;

                if (vertical_cell <= 0)
                    vertical_cell = 1;
                if (vertical_cell > h)
                    vertical_cell = h;

            }

            return new Vector2(horizontal_cell, vertical_cell);
        }

        /// <summary>
        /// identify the Tile in a provided cell
        /// </summary>
        /// <param name="X">cell address x</param>
        /// <param name="Y">cell address y</param>
        /// <returns>tile id</returns>
        public short find_tile_id_of_cell(int X, int Y)
        {
            if (valid_cell(X, Y))
                return world_map[X - 1, Y - 1].tile_id;
            else
                return 32767; // return tile id of the world edge
        }
        /// <summary>
        /// identify the Tile in a provided cell
        /// </summary>
        /// <param name="cell">cell address</param>
        /// <returns>tile id</returns>
        public short find_tile_id_of_cell(Vector2 cell)
        {
            return world_map[(int)cell.X - 1, (int)cell.Y - 1].tile_id;
        }
        /// <summary>
        /// Delete a group of cells
        /// </summary>
        /// <param name="engine">engine instance</param>    
        public void delete_group(Engine engine)
        {
            Vector2 start = get_current_hovered_cell(engine.get_current_mouse_state(), engine);
            delete_group_of_tiles(start, start, engine);
        }
        /// <summary>
        /// Add a group of tiles
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void add_group(Engine engine)
        {
            Vector2 start = get_current_hovered_cell(engine.get_current_mouse_state(), engine);
            add_group_of_tiles(start, start, engine);
        }
        /// <summary>
        /// delete a group of ui_elements ( version 1 - deletes every directly connected Tile of the same contexttype)
        /// </summary>
        /// <param name="current">current cell</param>
        /// <param name="original">original clicked cell</param>
        /// <param name="engine">engine instance</param>
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
        /// <summary>
        /// Add a group of tiles
        /// </summary>
        /// <param name="current">current cell</param>
        /// <param name="original">original clicked cell</param>
        /// <param name="engine">engine instance</param>
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
        /// <summary>
        /// calculate a tile variant rectangle for spritesheet
        /// </summary>
        /// <param name="variant"></param>
        /// <param name="cell"></param>
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
        /// <summary>
        /// calculate a corner rectangle in side the tile spritesheet
        /// </summary>
        /// <param name="variant">tile variant</param>
        /// <param name="cell">cell address</param>
        /// <param name="corner_number">1-4 corner</param>
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
        /// <summary>
        /// Determine connection config for internal corners and the tile variant for interconnected tiles
        /// </summary>
        /// <param name="cell">Cell address</param>
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
        /// <summary>
        /// Center the camera
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void center_camera_on_world_origin(Engine engine)
        {
            //center camera
            engine.set_camera_offset(Vector2.Zero);
        }
        /// <summary>
        /// Get the tile id based on cell
        /// </summary>
        /// <param name="source">cell address</param>
        /// <returns>tile id</returns>
        public short get_tile_id(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return world_map[(int)source.X - 1, (int)source.Y - 1].tile_id;
            }
            else
                return 999;
        }
        /// <summary>
        /// Get number of water units in a tile
        /// </summary>
        /// <param name="source">cell address</param>
        /// <returns>water content</returns>
        public float get_tile_water(Vector2 source)
        {
            if (valid_cell((int)source.X, (int)source.Y))
            {
                return world_map[(int)source.X - 1, (int)source.Y - 1].water_units;
            }
            else
                return 0;
        }
        /// <summary>
        /// calculate current sky color based on the  time and color ranges
        /// this function interpolates between colors of day phases based on the time of day (in minutes - for easier calculations)
        /// </summary>
        /// <param name="clock">world clock</param>
        /// <returns>Color of the sky based on world time</returns>
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
        /// <summary>
        /// calculates amount of ambient light based on time of day
        /// </summary>
        /// <param name="clock">world clock</param>
        /// <returns>ambient light value - lower = darker</returns>
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
        /// <summary>
        /// Convert pixel address to cell address
        /// </summary>
        /// <param name="pos">pixel coordinates</param>
        /// <returns>cell address</returns>
        public Vector2 vector_position_to_cell(Vector2 pos)
        {
            int X = ((int)pos.X) / tile_size + 1;
            int Y = ((int)pos.Y) / tile_size + 1;

            return new Vector2(X, Y);
        }

        /// <summary>
        /// cast colored circle from the light source
        /// </summary>
        /// <param name="engine">engine instance</param>
        /// <param name="sb">spritebatch</param>
        public void world_draw_point_lights(Engine engine, SpriteBatch sb)
        {
            Texture2D light_base = engine.get_texture("light_sphere_base");

            foreach (PointLight pl in world_lights)
            {
                engine.xna_draw(
                    light_base, 
                    pl.position - engine.get_camera_offset() - new Vector2(pl.active_radius() / 2, pl.active_radius() / 2), 
                    null, 
                    pl.get_color()*pl.get_intensity(), 
                    0, 
                    Vector2.Zero, 
                    (pl.get_radius()/1001f),//engine.fade_sine_wave_smooth(15000f,1f,1.025f), // slow pulsating
                    SpriteEffects.None, 
                    1f);
            }
        }
        /// <summary>
        /// get total lights in the world
        /// </summary>
        /// <returns>number of lights</returns>
        public int get_number_of_lights()
        {
            return world_lights.Count;
        }
        /// <summary>
        /// draw actual light objects
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void world_draw_point_light_sources(Engine engine)
        {
            // point light indicators should only be drawn if editor is active
            if (in_edit_mode())
            {
                // draw objects (light)
                int count = 0;
                // get textures
                Texture2D lsr = engine.get_texture("light_circle");
                Texture2D ls = engine.get_texture("light_source_indicator");
                // draw lights
                foreach (PointLight pl in world_lights)
                {
                    // draw the indicator
                    // for perfect rotation pixel height and width must be odd and the origin set at the exact center 
                    Vector2 object_offset = new Vector2(0,0);
                    engine.xna_draw(ls, pl.position - engine.get_camera_offset() - object_offset, null, pl.get_color(), Engine.cyclical_fade(4000f, 0.000000f, (float)Math.PI * 2.00000000f), new Vector2((float)ls.Width / 2, (float)ls.Height / 2),
                        Engine.fade_sine_wave_smooth(3000.0f * (2.0f - (pl.get_intensity() - 0.05f)), 0.45f, 1.25f), SpriteEffects.None, 1.0f); // fast pulsing if intense light

                    // draw light radius text
                    engine.xna_draw_outlined_text(pl.get_radius().ToString(), pl.position - engine.get_camera_offset() + new Vector2(20, 5), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    // draw information on hover                 
                    if(engine.show_light_pulses() || engine.are_vectors_equal(get_current_hovered_cell(engine.get_current_mouse_state(), engine), vector_position_to_cell(pl.position)))
                    {
                        // reach circle pulsating animation
                        // 1 - long travel of pulse
                        engine.xna_draw(
                            lsr, 
                            pl.position - engine.get_camera_offset() - object_offset, 
                            null, 
                            engine.adjusted_color(pl.get_color(), 0.65f)*0.25f, 
                            0f,
                            new Vector2((float)lsr.Width / 2, (float)lsr.Height / 2),
                            Engine.cyclical_fade(4000f * (2f - pl.get_intensity()), pl.get_radius() / 1400f, pl.get_radius() / 1000f), 
                            SpriteEffects.None, 
                            1.0f); // radius of the light reach (scale up the basic model
                        // 2 - maximum reach
                        engine.xna_draw(lsr, pl.position - engine.get_camera_offset() - object_offset, null, engine.adjusted_color(pl.get_color(), 0.55f) * 0.25f, Engine.cyclical_fade(3000.0f, 0.000000f, (float)Math.PI * 2.00000000f), new Vector2((float)lsr.Width / 2, (float)lsr.Height / 2),
                        pl.get_radius() / 1000f, SpriteEffects.None, 1.0f); // radius of the light reach (scale up the basic model
                        // 3 - short travel of pulse
                        engine.xna_draw(lsr, pl.position - engine.get_camera_offset() - object_offset, null, engine.adjusted_color(pl.get_color(), 0.85f) * 0.25f, 0f, new Vector2((float)lsr.Width / 2, (float)lsr.Height / 2),
                        Engine.cyclical_fade(1000f * (2f - pl.get_intensity()), pl.get_radius() / 14000f, pl.get_radius() / 1400f), SpriteEffects.None, 1.0f); // radius of the light reach (scale up the basic model
                        
                        // text info on the radius
                        engine.xna_draw_outlined_text("pos:"+pl.cell+"light color: " + pl.get_color().ToString() + " radius: " + pl.active_radius().ToString() + " intensity: " + pl.get_intensity().ToString(),
                         pl.position - engine.get_camera_offset() - object_offset + new Vector2(20, 0), Vector2.Zero, Color.Yellow, Color.Black, engine.get_UI_font());
                    }
                    count++;
                }
            }
        }
        /// <summary>
        /// draw water generator sprites
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void world_draw_water_generators(Engine engine)
        {
            // water source indicators should only be drawn if editor is active
            if (in_edit_mode())
            {
                // draw objects (light)
                int count = 0;
                foreach (WaterGenerator pl in wsources)
                {
                    // draw the indicator
                    Vector2 object_offset = new Vector2(pl.sprite.Width / 2, pl.sprite.Height / 2);
                    engine.xna_draw(pl.sprite, pl.get_position() - engine.get_camera_offset() - object_offset, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1.0f);
                    count++;
                }
            }
        }
        /// <summary>
        /// get the world name
        /// </summary>
        /// <returns>string representation of the world name</returns>
        public string get_world_name()
        {
            return world_name;
        }
        /// <summary>
        /// serialize world objects
        /// Tree
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void serialize_tree_data(Engine engine)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            ArrayList trees_save = new ArrayList();

            foreach (Tree tree in trees)
            {
                Tree_Data obj = new Tree_Data();
                obj.name_modifier = tree.get_name_modifier();
                obj.origin_x = tree.get_position().X;
                obj.origin_y = tree.get_position().Y;
                obj.base_variant = tree.get_base_variant();
                obj.crown_variant = tree.get_crown_variant();
                obj.max_trunks = tree.get_max_trunks();
                obj.tint_r = (int)tree.get_tint_color(engine).R;
                obj.tint_g = (int)tree.get_tint_color(engine).G;
                obj.tint_b = (int)tree.get_tint_color(engine).B;
                obj.tint_factor = tree.get_tint_factor();

                // save the object
                trees_save.Add(obj);
            }
            // write xml
            using (XmlWriter writer = XmlWriter.Create(this.world_name+"_trees.xml", settings))
            {
                IntermediateSerializer.Serialize(writer, trees_save, null); // write complete arraylist to the xml file
            }
        }
        /// <summary>
        /// serialize world objects
        /// Lights
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void serialize_light_data(Engine engine)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            ArrayList light_save = new ArrayList();

            foreach (PointLight p in world_lights)
            {
                Light_Data obj = new Light_Data();
                obj.origin_x = p.position.X;
                obj.origin_y = p.position.Y;
                obj.cell_x = p.cell.X;
                obj.cell_y = p.cell.Y;
                obj.intensity = p.get_intensity();
                obj.radius = p.get_radius();
                obj.tint_r = p.get_color().R;
                obj.tint_g = p.get_color().G;
                obj.tint_b = p.get_color().B;
                // save the object
                light_save.Add(obj);
            }
            // write xml
            using (XmlWriter writer = XmlWriter.Create(this.world_name + "_lights.xml", settings))
            {
                IntermediateSerializer.Serialize(writer, light_save, null); // write complete arraylist to the xml file
            }
        }
        /// <summary>
        /// serialize world objects
        /// Grass
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void serialize_grass_data(Engine engine)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            ArrayList grass_save = new ArrayList();

            foreach (Grass grass in grass_tiles)
            {
                Grass_Data obj = new Grass_Data();
                obj.origin_x = grass.get_cell_address().X;
                obj.origin_y = grass.get_cell_address().Y;

                // save the object
                grass_save.Add(obj);
            }
            // write xml
            using (XmlWriter writer = XmlWriter.Create(this.world_name + "_grass.xml", settings))
            {
                IntermediateSerializer.Serialize(writer, grass_save, null); // write complete arraylist to the xml file
            }
        }
        /// <summary>
        /// serialize world objects
        /// Water Generators
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void serialize_watergen_data(Engine engine)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            ArrayList water_save = new ArrayList();

            foreach (WaterGenerator w in wsources)
            {
                WaterData obj = new WaterData();
                obj.cell_x = w.get_cell_address().X;
                obj.cell_y = w.get_cell_address().Y;
                obj.pos_x = w.get_position().X;
                obj.pos_y = w.get_position().Y;
                obj.intensity = w.get_intensity();
                // save the object
                water_save.Add(obj);
            }
            // write xml
            using (XmlWriter writer = XmlWriter.Create(this.world_name + "_watergen.xml", settings))
            {
                IntermediateSerializer.Serialize(writer, water_save, null); // write complete arraylist to the xml file
            }
        }
    }
}