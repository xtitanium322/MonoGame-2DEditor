using System;
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
using System.Globalization;                                                 // various string formats
using System.Diagnostics;                                                   // Debug
using System.Xml;                                                           // use xml files
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;  // xml serialization
using System.Runtime.Serialization.Formatters.Binary;
using MyDataTypes;                                                          // data types structure
using System.IO;
using System.Windows.Forms;                                                 // Windows keyboard input

namespace EditorEngine
{
    /// <summary>
    /// Engine class - controls most core functionalitites of the program, enables connection between Game and rendering with other custom classes.
    /// </summary>
    public class Engine
    {
        /// <summary>
        /// Value timer tracks the value over time (for changes in certain values other than time itself)
        /// </summary>
        /// <typeparam name="T">Type of the value being checked</typeparam>
        private struct ValueTimer<T>                                      
        {
            public long millisecond_checkpoint;             // last checkpoint
            public T checkpoint_value;                      // value being tracked by this timer
            public int checkpoint_rate;                     // number of milliseconds before assigning new checkpoint value
            public ValueTimer(T checkpoint_value, int rate)
                : this()
            {
                this.checkpoint_value = checkpoint_value;
                checkpoint_rate = rate;
                millisecond_checkpoint = 0;
            }
        }
        /// <summary>
        /// Construct to keep track of all graphic elements loaded for future use.
        /// Enables easy lookup by name
        /// </summary>
        private class texture_element
        {
            Texture2D texture;
            string name;
            public texture_element(Texture2D t, string n)
            {
                texture = t;
                name = n;
            }
            public string get_name()
            {
                return name;
            }
            public Texture2D get_texture()
            {
                return texture;
            }
        }
// Engine variables
        private const int CLOUD_CAPACITY = 30;                             // maximum number of clouds for a single world. Each cloud has 10 paricle emmiters
        public static Texture2D pixel = Game1.pixel_texture;               // standard white pixel used to draw most primitives
        public static Rectangle standard20 = new Rectangle(0, 0, 20, 20);  // standard size 20 px rectangle
        private Game1 refgame;                                             // reference to Game1 object
        private ParticleEngine particle_engine;                            // Test object for particle system - allows quick test of various particle parameters to assit in effect creation.
        public static float gravity_value = 10f;                           // move 10 pixels per second (acceleration)
        public Wind wind;                                                  // Wind object. Responsible for horizontal movement of affected particles.
        private int rain_chance;                                           // chance of any cloud becoming a rain cloud (generating rain particles)
        private long last_rain_update;                                     // when the last rain probability calculation occurred
        public WorldClock clock;                                           // in-game clock - no relation to real world clock other than the format
        private int world_clock_rate;                                      // number of frames to Update world clock
        private int world_clock_multiplier;                                // number of seconds added per Update cycle
        private bool paused;                                               // is the game paused?
        private bool lighting_enabled;                                     // calculate and display lighting in the game world
        private int frames;                                                // total number of frames drawn/updated
        private float fps;                                                 // frames per second
        private int draw_calls = 0;                                        // number of times xna draw has been called since last Update
        private long draw_calls_total = 0;                                 // total number of xna draw calls since game launch
        private float prevWheelValue, currWheelValue;                      // mousewheel values
        private int current_game_second;                                   // real time since start of the game in : seconds
        private static long current_game_millisecond;                      // real time since start of the game in : milliseconds
        private bool camera_moving;                                        // camera movement flag
        private long camera_movement_start;                                // calculate when camera movement started
        private Vector2 camera_offset;                                     // controls the camera
        private int camera_speed = 20;                                     // for simple camera movement - number of pixels to move (important: match tile_size for smooth line movement)
        private int update_step = 8;                                       // limit number of milliseconds it takes to move camera again
        static Random rng;                                                 // Random number generator object 
        SpriteBatch sb;                                                    // spritebatch object
        Viewport viewport;                                                 // viewport object
        private List<texture_element> all_textures;                        // list of frequently used named textures
        private SpriteFont UIfont;                                         // font assigned to GUI       
        private ValueTimer<long> update_timer;                             // last time an update ran in Engine
        private ParticleTester particle_generator;                         // testing various particle creation at the mouse pointer
        public TextEngine engine_text_engine;                              // Text Engine object used to display statistics
        // keyboard and mouse
        private KeyboardState keyboardState;                               // current state of the keyboard
        private KeyboardState OldKeyboardState;                            // previous state of the keyboard
        private MouseState mouseState;                                     // current mouse state
        private MouseState OldMouseState;                                  // previous mouse state
        private WorldCollection world_list;                                // All the world objects with tile order filename included
        private Editor editor;                                             // Editor class - for adding/deleting/selecting/tweaking ui_elements,lights etc. (originally in Editor - now will exists as a universal engine feature - pass current world as a paramater)
        public float grid_transparency_value;                              // used to display cell placement grid
        public int gridcolor_r;                         
        public int gridcolor_g;
        public int gridcolor_b;
        private Color grid_color;                                          // used to draw editor grid
        public List<Cloud> clouds;                                         // list of all the clowds (shared among all worlds)
        private bool light_pulses_flag = false;                            // show or hide light reach information animation
        private int deferred_light_generation_order = 0;                   // number of lights that must be generated, must be spread over multiple frames so that the program doesn't freeze while textures are built
        private long last_input_time = 0;                                  // for keyboard input repeating rate
        public bool input_repeated = false;                                // flag set to initiate a repeat
        private List<Container> deserialized_containers = null;            // deserialized container information goes here - previous state re-used
        private Editor deserialized_editor = null;                         // editor object generated from the save file
        ValueTimer<int> FPStracker;                                        // instance of timer tracking framerate changes
       
        // Singleton constructor section
        private static Engine instance;                                    // ensure there is only one instance
        public static Engine get_instance(ref SpriteBatch sb, ref Viewport vp, Game1 g)
        {
            if (instance == null)
            {
                instance = new Engine(ref sb, ref vp, g);
            }
            return instance;
        }
        /// <summary>
        /// private constructor ensuring a single instance of Engine
        /// </summary>
        /// <param name="sb">spritebatch instance reference</param>
        /// <param name="vp">viewport reference</param>
        /// <param name="g">Game object</param>
        private Engine(ref SpriteBatch sb, ref Viewport vp, Game1 g)
        {
            refgame = g;
            world_clock_rate = 20;         // larger number = slower time
            world_clock_multiplier = 15;   // number of seconds added per Update. more = faster clock
            rain_chance = 0;
            last_rain_update = 0;
            clock = new WorldClock(world_clock_rate, world_clock_multiplier);
            paused = false;
            lighting_enabled = true;
            frames = 0;
            fps = 0.0f;
            prevWheelValue = currWheelValue = 0.0f;
            current_game_second = 0;
            current_game_millisecond = 0;
            camera_offset = new Vector2(0, 0);
            this.sb = sb;
            this.viewport = vp;
            rng = new Random();
            wind = new Wind();
            all_textures = new List<texture_element>();
            FPStracker = new ValueTimer<int>(frames, 250); // check for the value of frame every X milliseconds
            camera_moving = false;
            update_timer = new ValueTimer<long>();
            keyboardState = Keyboard.GetState();
            OldKeyboardState = keyboardState;
            mouseState = Mouse.GetState();
            OldMouseState = mouseState;
            grid_transparency_value = 0.25f;
            gridcolor_r = 255;
            gridcolor_g = 255;
            gridcolor_b = 255;
            grid_color = new Color(gridcolor_r, gridcolor_g, gridcolor_b);
            world_list = new WorldCollection();
            editor = new Editor(this);
            engine_text_engine = new TextEngine(get_UI_font(), Rectangle.Empty, Vector2.Zero, Color.White);
            particle_engine = new ParticleEngine();
            particle_generator = new ParticleTester();
            clouds = new List<Cloud>();  
        }
        /// <summary>
        /// Load textures, deserialize saved objects
        /// </summary>
        /// <param name="content">ContentManager</param>
        public void LoadContent(ContentManager content)
        {
            all_textures.Add(new texture_element(content.Load<Texture2D>("emitter_indicator"), "emitter_indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_addmode"), "icon-addmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_deletemode"), "icon-deletemode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_selectmode"), "icon-selectmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_lightsmode"), "icon-lightsmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("slider_line"), "item-sliderline"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("active_indicator"), "icon-active-indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("expansion_indicator"), "icon-expansion-indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("custom_200x30"), "200x30_background1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("200x30custom2"), "200x30_background2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("custom_240x30"), "240x30_background1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("overlay_deleted_cell"), "deleted_indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("slider_progress"), "slider_progress"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("ghost_scroller_top"), "ghost_scroller_top"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("ghost_scroller_bottom"), "ghost_scroller_bottom"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse"), "mouse_default")); // default mouse
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse_hand"), "mouse_hand")); // special mouse (hover)
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse_move_indicator"), "mouse_move_indicator")); // special mouse (GUI move mode)
            all_textures.Add(new texture_element(content.Load<Texture2D>("progress200x20"), "progress_mask"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("progress200x20border"), "progress_border"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lock_icon"), "editor_icon_locked"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lock_open_icon"), "editor_icon_unlocked"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("light_source"), "light_source_indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("light_source_outer"), "light_source_outer"));
            all_textures.Add((new texture_element(Game1.createOpaqueCircle(1001, Color.White, 0.05f),"light_circle"))); // create a reach model (outline of the reach circle
            all_textures.Add((new texture_element(Game1.createSmoothCircle(1001, Color.White, 1f), "light_sphere_base"))); // scaled to fit the light reach, color multiplied by light intensity to fit the radiance value
            all_textures.Add(new texture_element(content.Load<Texture2D>("tree_base1"), "tree_base1")); // first version of the tree base - testing sprite - will convert to sprite-sheet
            all_textures.Add(new texture_element(content.Load<Texture2D>("base2"), "tree_base2")); // first version of the tree base - testing sprite - will convert to sprite-sheet
            all_textures.Add(new texture_element(content.Load<Texture2D>("base3"), "tree_base3")); // first version of the tree base - testing sprite - will convert to sprite-sheet
            all_textures.Add(new texture_element(content.Load<Texture2D>("trunk1"), "trunk1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("trunk2"), "trunk2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch1"), "lbranch1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch2"), "lbranch2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch3"), "lbranch3"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch4"), "lbranch4"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch5"), "lbranch5"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch6"), "lbranch6"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lbranch7"), "lbranch7"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("branch1"), "branch1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("branch2"), "branch2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("branch3"), "branch3"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("branch4"), "branch4"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("branch5"), "branch5"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("leaves1"), "leaves1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("leaves2"), "leaves2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("leaves3"), "leaves3"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("grass1"), "grass1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("grass2"), "grass2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("grass_corner_top"), "grass_corner_top"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("grass_single"), "grass_single"));

            all_textures.Add(new texture_element(content.Load<Texture2D>("palmtrunk1"), "palmtrunk1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("palmbase1"), "palmtree_base1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("palmleaves1"), "palmleaves1"));

            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud1"), "cloud1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud2"), "cloud2"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud3"), "cloud3"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud4"), "cloud4"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud5"), "cloud5"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud6"), "cloud6"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud7"), "cloud7"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud8"), "cloud8"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("cloud9"), "cloud9"));

            all_textures.Add(new texture_element(content.Load<Texture2D>("raindrop"), "raindrop"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("star_particle"), "star"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("square_particle"), "square"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("hollow_square_particle"), "hollow_square"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("x_particle"), "x"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("triangle_particle"), "triangle"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("circle_particle"), "circle"));

            world_list.add_world("world1.xml", new World(this, content, "world_1", 300, 100));
            world_list.add_world("world2.xml", new World(this, content, "world_2", 100, 50));
            world_list.add_world("world3.xml", new World(this, content, "world_3", 500, 400));
            world_list.load_tiles(this); // load all the xml files containing world tile info

            editor.LoadContent(content, this); // loads default user interface into the editor
            // deserializes previously saved user interface
            deserialize_data<List<Container>>("user_interface.bin"); // user interface  container list
            deserialize_data<Editor>("editor.bin");                  // selected editor variables
            deserialize_data<Editor>("editor.bin");                  // selected editor variables
            // update necessary values using deserialized list of user interface containers and their inner elements 
            editor.seed_interface_with_serialized_data(deserialized_containers);
            editor.seed_interface_with_color_data(deserialized_editor);

            engine_text_engine.set_target(((TextArea)editor.GUI.find_element("TEXTAREA_STATISTICS")).get_rectangle(), ((TextArea)editor.GUI.find_element("TEXTAREA_STATISTICS")).get_origin());
            engine_text_engine.set_font(get_UI_font());                     // set the font 

            // create particle tester
            this.get_particle_engine().create_emitter(
                particle_generator,                 // host
                new Vector2[]{new Vector2(mouseState.X,mouseState.Y)},// array of coordinates for initial position
                particle_type.x,                    // particle type
                false,                              // random particles
                -1,                                 // emitter lifetime
                1,                                  // n = rate of creation, every n ms
                true,                               // auto generation
                trajectory_type.chaotic,            // trajectory
                300,                                // test particles will begin at 300 ms lifetime
                12,                                 // n = burst value - n per burst
                false,                              // random color
                true,                               // color interpolation
                1);                                 // radius - offset from the emitter coordinate for creation - horizontal

            // register in Particle Engine to enable particle generation
            this.get_particle_engine().register_emitter_enabled_object(particle_generator);
        }
        /// <summary>
        /// Unload all content and serialize important data
        /// </summary>
        public void UnloadContent()
        {   // finalize objects when game is closed
            List<Container> a = get_editor().GUI.get_all_containers();

            serialize_data<List<Container>>(a, "user_interface.bin");
            serialize_data<Editor>(editor, "editor.bin");

            // serialize trees, grass
            foreach(WorldStruct w in world_list.get_worlds())
            {
                w.world.serialize_tree_data(this);
                w.world.serialize_grass_data(this);
                w.world.serialize_light_data(this);
                w.world.serialize_watergen_data(this);
            }
        }
 
        /// <summary>
        /// engine class main Update function
        /// </summary>
        public void Update()
        {
            clock.update(current_game_millisecond);
            // Update get_fps timer
            if ((current_game_millisecond - FPStracker.millisecond_checkpoint) >= FPStracker.checkpoint_rate && ((float)FPStracker.checkpoint_rate / 1000f) > 0.0f)
            {
                fps = (float)(frames - FPStracker.checkpoint_value) / ((float)FPStracker.checkpoint_rate / 1000f);

                FPStracker.checkpoint_value = frames;
                FPStracker.millisecond_checkpoint = current_game_millisecond;
            }
            // update clouds
            foreach (Cloud cloud in clouds)
            {
                cloud.Update(this);
            }

            // remove clouds
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                if (clouds[i].is_removable())
                {
                    clouds.Remove(clouds[i]);
                }
            }
            // add missing clouds
            if (clouds.Count < CLOUD_CAPACITY)
            {
                for (int i = 0; i < (CLOUD_CAPACITY - clouds.Count); i++)
                {
                    Cloud temp_cloud = new Cloud(this); // automatically creates a cloud within current active world width boundary

                    // create emitter location array
                    // several emitter locations for heavy rain effect
                    Vector2[] array = new Vector2[] {                     
                            temp_cloud.get_position() + new Vector2(5, 0), 
                            temp_cloud.get_position() - new Vector2(5, 0),

                            temp_cloud.get_position() + new Vector2(15,0),
                            temp_cloud.get_position() - new Vector2(15,0), 

                            temp_cloud.get_position() + new Vector2(25,0),
                            temp_cloud.get_position() - new Vector2(25,0),

                            temp_cloud.get_position() + new Vector2(35,0),
                            temp_cloud.get_position() - new Vector2(35,0),

                            temp_cloud.get_position() + new Vector2(45,0),
                            temp_cloud.get_position() - new Vector2(45,0)
                        };

                    this.get_particle_engine().create_emitter(
                        temp_cloud,                         // host
                        array,                              // array of coordinates for initial position
                        particle_type.raindrop,             // particle type
                        true,                               // random particles
                        3000,                               // emitter lifetime
                        300,                                // rate of creation
                        false,                              // not auto generation
                        trajectory_type.fall,               // trajectory
                        Engine.generate_int_range(900, 2500),// rain lifetime
                        3,                                  // burst value
                        false,                              // random color
                        false,                              // no color interpolation
                        5);                                 // radius - offset from the emitter coordinate for creation

                    // register in Particle Engine to enable particle generation
                    this.get_particle_engine().register_emitter_enabled_object(temp_cloud);

                    // add cloud to the list
                    clouds.Add(temp_cloud);
                }
            }
            // update rain chance every 25 seconds, only if last 25 seconds werw ithout rain
            if (current_game_millisecond - last_rain_update > 25000)
            {
                if (rain_chance > 0)
                    rain_chance = 0; // stop rain every update cycle
                else
                    rain_chance = generate_int_range(0, 100); // random chance of rain

                last_rain_update = current_game_millisecond;
            }
            // update wind
            wind.Update();
            // particle tester update
            particle_generator.update_emitter_position(new Vector2(mouseState.X,mouseState.Y) + camera_offset); // where mouse pointer is - also account for camera offset, since this is not bound to game world
            // update Particle Engine
            particle_engine.update(this);
            // update GUI option connected values
            grid_transparency_value = editor.GUI.get_slider_value(actions.update_slider_grid_transparency);
            gridcolor_r = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_red);
            gridcolor_g = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_green);
            gridcolor_b = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_blue);
            grid_color = new Color(gridcolor_r, gridcolor_g, gridcolor_b);

            editor.Update(this);
            engine_text_engine.set_target(((TextArea)editor.GUI.find_element("TEXTAREA_STATISTICS")).get_rectangle(), ((TextArea)editor.GUI.find_element("TEXTAREA_STATISTICS")).get_origin());
            engine_text_engine.update(); // need to update to create message lines 
            
            // generate enqueued lights that were ordered 
            // limit to a few per frame so that there is no huge slowdown in rendering
            int limit = 1;
            int pos_x = 0;
            int pos_y = 0;

            while(deferred_light_generation_order > 0)
            {
                // find an unoccupied cell, while there is space in the world
                while (this.get_world_list().get_current().get_number_of_lights() < get_current_world().get_world_size())
                {
                    // generate random coordinates
                    pos_x = generate_int_range(1, get_current_world().width);
                    pos_y = generate_int_range(1, get_current_world().height);
                    // check if light exists
                    if(!get_current_world().is_light_object_in_cell(new Vector2(pos_x,pos_y)))
                    {
                        break;
                    }
                }

                get_current_world().generate_light_source(new Vector2(pos_x, pos_y), new Color(generate_int_range(0, 255), generate_int_range(0, 255), generate_int_range(0, 255)), get_current_mouse_state(), this, generate_int_range(300, 800), generate_float_range(0.15f, 1.35f));
                
                deferred_light_generation_order--;
                limit--;

                this.get_editor().GUI.get_text_engine().add_message_element(this, "system[0,75,220] ( ~time ): " + deferred_light_generation_order + " lights remaining");
                // stop generating for now
                if (limit == 0)
                    break;
            }

            // tree growth - for any Tree inheriting class
            foreach(Tree t in world_list.get_current().trees)
            {
                t.generate_trunk(this);
            }
        }
        /// <summary>
        /// get particleengine object
        /// </summary>
        /// <returns>ParticleEngine object</returns>
        public ParticleEngine get_particle_engine()
        {
            return particle_engine;
        }
        /// <summary>
        /// ParticleEngine emitter - update particle type
        /// </summary>
        /// <param name="p_type">particle type</param>
        public void change_test_particle_shape(particle_type p_type)
        {
            foreach(Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_particle_type(p_type);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update base color
        /// </summary>
        /// <param name="tint">Color value</param>
        public void change_particle_base_tint(Color tint)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_particle_color(tint);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update particle duration
        /// </summary>
        /// <param name="val">milliseconds value</param>
        public void change_particle_lifetime(int val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_particle_lifetime(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update particle emitter burst zone radius
        /// </summary>
        /// <param name="val">number of pixels</param>
        public void change_particle_emitter_radius(int val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_emitter_radius(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - udpate number of particles generated in one burst
        /// </summary>
        /// <param name="val">number of pixels</param>
        public void change_particle_burst_amount(int val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_emitter_burst_amount(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update angular velocity
        /// </summary>
        /// <param name="val">radians value</param>
        public void change_particle_rotation_amount(float val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.update_rotation_amount(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update particle texture scale 
        /// </summary>
        /// <param name="val">a float value where 1f means 100% original scale</param>
        public void change_particle_scale(float val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.update_scale(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update creation rate. number of milliseconds between bursts
        /// </summary>
        /// <param name="val">number of milliseconds</param>
        public void change_particle_creation_rate(int val)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.update_creation_rate(val);
            }
        }
        /// <summary>
        /// ParticleEngine emitter - update particle movement trajectory
        /// </summary>
        /// <param name="t_type">trajectory type</param>
        public void change_particle_trajectory(trajectory_type t_type)
        {
            foreach (Emitter e in particle_generator.get_particle_emitters())
            {
                e.change_trajectory_type(t_type);

                switch (t_type)
                {
                    case trajectory_type.fall:
                        e.set_emitter_acceleration(Engine.gravity_value); // set gravity aceleration
                        break;
                    case trajectory_type.chaotic:
                        e.set_emitter_acceleration(0f); 
                        break;
                    case trajectory_type.rise:
                        e.set_emitter_acceleration(Engine.generate_float_range(5f,10f)); // random rise acceleration
                        break;
                    case trajectory_type.ballistic_curve:
                        e.set_emitter_acceleration(Engine.gravity_value);                // gravity 
                        break;
                    case trajectory_type.static_:
                        e.set_emitter_acceleration(0f);                                  // no acceleration 
                        break;
                    case trajectory_type.laser_line:
                        e.set_emitter_acceleration(0f);                                  // no acceleration 
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// update viewport
        /// </summary>
        /// <param name="v">new viewport object</param>
        public void refresh_viewport(ref Viewport v)
        {
            viewport = v;
        }
        /// <summary>
        /// Enables particle generation for Engine's particle emitting object
        /// </summary>
        /// <param name="value">true for enabling, false for disabling</param>
        public void enable_particle_test(bool value)
        {
            if (!value)
                particle_generator.disable_particle_signal();
            else
                particle_generator.generate_particle_signal();
        }
        /// <summary>
        /// Enable particle color interpolation
        /// </summary>
        /// <param name="value">true or false</param>
        /// <param name="clr">secondary color to change to</param>
        public void enable_particle_color_interpolation(bool value, Color clr)
        {
            if (!value)
                particle_generator.disable_color_interpolation();
            else
                particle_generator.enable_color_interpolation(clr);
        }
        /// <summary>
        /// Draw - rendering function
        /// </summary>
        /// <param name="spb">spritebatch object</param>
        /// <param name="current">current active world</param>
        public void Draw(SpriteBatch spb, World current)
        {
            // based on inerface and game states draw different mouse sprites
            // not in a move mode
            if (!editor.gui_move_mode)
            {
                if (editor.GUI.hover_detect() && current.in_edit_mode()) // hovered
                {
                    xna_draw(this.get_texture("mouse_hand"), get_mouse_vector(), null, negative_color(editor.get_interface_color()), 0f, new Vector2(18, 18), 1f, SpriteEffects.None, 1f);
                }
                else // not-hovered
                {
                    xna_draw(this.get_texture("mouse_default"), get_mouse_vector(), null, negative_color(editor.get_interface_color()), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                }
            }
            // move mode
            else
            {
                xna_draw(this.get_texture("mouse_move_indicator"), get_mouse_vector(), null, negative_color(editor.get_interface_color()), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            }
            // draw statistics 
            // show text in the text area
            if (editor.GUI.find_container("CONTAINER_STATISTICS_TEXT_AREA").is_visible())
                engine_text_engine.textengine_draw(this, true);
        }
        /// <summary>
        /// Draw clouds - in separate function for easier layering (across render targets if needed)
        /// </summary>
        public void draw_clouds()
        {
            //------ Cloud loop
            foreach (Cloud c in clouds)
            {
                Texture2D cloud_sprite = this.get_texture("cloud" + c.get_cloud_variant());
                //check fadeout value and opacity value
                float factor = 1f;
                float opacity_factor = c.get_opacity();

                if (c.is_fading_in())
                    factor = (float)c.get_fadeout_based_scale_factor(this);

                if (c.is_fading_out())
                    factor = 1f - (float)c.get_fadeout_based_scale_factor(this);

                //draw
                this.xna_draw(cloud_sprite, c.get_position() - this.get_camera_offset(),
                    null,
                    (c.is_a_rain_cloud() == true ? Color.LightSkyBlue : Color.White) * opacity_factor, // color rain cloud with a darker shade 
                    0f,
                    new Vector2(cloud_sprite.Width / 2, cloud_sprite.Height / 2),
                    factor,
                    SpriteEffects.None,
                    1f);
            }
        }
        /// <summary>
        /// Get current editor object 
        /// </summary>
        /// <returns>Editor editor</returns>
        public Editor get_editor()
        {
            return editor;
        }
        /// <summary>
        /// Get world collection
        /// </summary>
        /// <returns>WorldCollection object</returns>
        public WorldCollection get_world_list()
        {
            return world_list;
        }
        /// <summary>
        /// Get currently active world
        /// </summary>
        /// <returns>World object</returns>
        public World get_current_world()
        {
            return world_list.get_current();
        }
        /// <summary>
        /// Get current world clock
        /// </summary>
        /// <returns>WorldClock object</returns>
        public WorldClock get_clock()
        {
            return clock;
        }
        /// <summary>
        /// Set spritefont to be used by xna_draw_text() variations
        /// </summary>
        /// <param name="font">Spritefont object </param>
        public void set_UI_font(SpriteFont font)
        {
            UIfont = font;
        }
        /// <summary>
        /// Get access to current spritefont
        /// </summary>
        /// <returns>Spritefont object</returns>
        public SpriteFont get_UI_font()
        {
            return UIfont;
        }
        /// <summary>
        /// Get current editor grid color
        /// </summary>
        /// <returns>Color value</returns>
        public Color get_grid_color()
        {
            return grid_color;
        }
        /// <summary>
        /// return camera movement flag
        /// </summary>
        /// <returns></returns>
        public bool is_camera_moving()
        {
            return camera_moving;
        }
        /// <summary>
        /// Update camera movement flag
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_camera_movement_flag(bool value)
        {
            camera_moving = value;
        }
        /// <summary>
        /// get current mouse + keyboard states
        /// </summary>
        public void refresh_keyboard_and_mouse()
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
        }
        /// <summary>
        /// Update previosu mouse + keyboard states
        /// </summary>
        public void save_previous_keyboard_and_mouse()
        {
            OldKeyboardState = keyboardState;
            OldMouseState = mouseState;
        }
        /// <summary>
        /// Get current mouse state
        /// </summary>
        /// <returns>MouseState</returns>
        public MouseState get_current_mouse_state()
        {
            return mouseState;
        }
        /// <summary>
        /// Get current keyboard state
        /// </summary>
        /// <returns>KeyboardState</returns>
        public KeyboardState get_current_keyboard_state()
        {
            return keyboardState;
        }
        /// <summary>
        /// Get prev mouse state
        /// </summary>
        /// <returns>MouseState</returns>
        public MouseState get_previous_mouse_state()
        {
            return OldMouseState;
        }
        /// <summary>
        /// Get prev keyboard state
        /// </summary>
        /// <returns>KeyboardState</returns>
        public KeyboardState get_previous_keyboard_state()
        {
            return OldKeyboardState;
        }
        /// <summary>
        /// Find texture by name
        /// </summary>
        /// <param name="name">lookup value</param>
        /// <returns>texture or a null if not found</returns>
        public Texture2D get_texture(String name)
        {
            foreach (texture_element t in all_textures)
            {
                if (t.get_name() == name)
                    return t.get_texture();
            }
            return null;
        }
        /// <summary>
        /// return current position of a mouse as a 1 cell collision rectangle
        /// </summary>
        /// <returns></returns>
        public Rectangle get_mouse_rectangle()
        {
            return new Rectangle(mouseState.X, mouseState.Y, 1, 1);
        }
        /// <summary>
        /// Gets current mouse psition on screen
        /// </summary>
        /// <returns>Current mouse position</returns>
        public Vector2 get_mouse_vector()
        {
            return new Vector2(mouseState.X, mouseState.Y);
        }
        /// <summary>
        /// Function that calculates a difference between current and old mouse position
        /// </summary>
        /// <returns>Mouse position delta between frames</returns>
        public Vector2 get_mouse_displacement()
        {
            return new Vector2(mouseState.X - OldMouseState.X, mouseState.Y - OldMouseState.Y);
        }
// rendering functions section
        /// <summary>
        /// Draw text on screen
        /// </summary>
        /// <param name="target_string">what text to draw</param>
        /// <param name="font_position">where to draw</param>
        /// <param name="font_rotation_point">rotate the string using this as a center point</param>
        /// <param name="c">text color</param>
        /// <param name="font">font to use</param>
        public void xna_draw_text(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color c, SpriteFont font)
        {
            sb.DrawString(font, target_string, font_position, c, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            draw_calls++;
        }
        /// <summary>
        /// Draw text on screen with background
        /// </summary>
        /// <param name="target_string">what text to draw</param>
        /// <param name="font_position">where to draw</param>
        /// <param name="font_rotation_point">rotate the string using this as a center point</param>
        /// <param name="c">text color</param>
        /// <param name="font">font to use</param>
        /// <param name="padding">padding value - pixels between border and text</param>
        public void xna_draw_text_background(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color c, SpriteFont font, int padding = 2)
        {
            Vector2 size = font.MeasureString(target_string);
            // draw background
            xna_draw_rectangle(new Rectangle((int)font_position.X-padding, (int)font_position.Y-padding, (int)size.X+padding*2, (int)size.Y+padding*2), Color.Black, 1, 0.7f);
            // draw string
            sb.DrawString(font, target_string, font_position, c, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            draw_calls += 2;
        }
        /// <summary>
        /// Draw text on screen but cut it from either side to fit a shape
        /// </summary>
        /// <param name="target_string">what text to draw</param>
        /// <param name="font_position">where to draw</param>
        /// <param name="font_rotation_point">rotate the string using this as a center point</param>
        /// <param name="c">text color</param>
        /// <param name="font">font to use</param>
        /// <param name="crop">crop rectangle - visible portion</param>
        public void xna_draw_text_crop_ui(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color text_color, SpriteFont font, Rectangle crop)
        {
            sb.End(); // end any current spritebatch

            RasterizerState _rasterizerState = new RasterizerState() { ScissorTestEnable = true };
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, _rasterizerState);
            Rectangle current = sb.GraphicsDevice.ScissorRectangle; // save current

            sb.GraphicsDevice.ScissorRectangle = crop; //set new

            sb.DrawString(font, target_string, font_position, text_color, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f); // text itself

            sb.GraphicsDevice.ScissorRectangle = current; // reset old scissor rectangle

            sb.End(); // end current modified spritebatch

            draw_calls += 5;
            // reset with standard values
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend); // calls spritebatch back in with the static ui values
        }
        /// <summary>
        /// Draw text on screen and add an outline around it
        /// </summary>
        /// <param name="target_string">text to draw</param>
        /// <param name="font_position">where to draw</param>
        /// <param name="font_rotation_point">if text is rotated - use this as center point</param>
        /// <param name="text_color">text color</param>
        /// <param name="outline">outline color</param>
        /// <param name="font">font to be used</param>
        /// <param name="scale">scale the text up or down using this float value</param>
        public void xna_draw_outlined_text(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color text_color, Color outline, SpriteFont font,float scale = 1f)
        {
            sb.DrawString(font, target_string, font_position + new Vector2(1f, 0), outline, 0.0f, font_rotation_point, scale, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(1f, 0), outline, 0.0f, font_rotation_point, scale, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position + new Vector2(0, 1f), outline, 0.0f, font_rotation_point, scale, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(0, 1f), outline, 0.0f, font_rotation_point, scale, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position, text_color, 0.0f, font_rotation_point, scale, SpriteEffects.None, 1.0f); // text itself

            draw_calls += 5;
        }
        /// <summary>
        /// Draw outline text cropped to fit a rectangle area
        /// </summary>
        /// <param name="target_string">text to draw</param>
        /// <param name="font_position">where to draw</param>
        /// <param name="font_rotation_point">if text is rotated - use this as center point</param>
        /// <param name="text_color">text color</param>
        /// <param name="outline">outline color</param>
        /// <param name="font">font to be used</param>
        /// <param name="crop">crop rectangle</param>
        public void xna_draw_outlined_text_crop_ui(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color text_color, Color outline, SpriteFont font, Rectangle crop)
        {
            sb.End(); // end any current spritebatch

            RasterizerState _rasterizerState = new RasterizerState() { ScissorTestEnable = true };
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, _rasterizerState);
            Rectangle current = sb.GraphicsDevice.ScissorRectangle; // save current

            sb.GraphicsDevice.ScissorRectangle = crop; //set new

            sb.DrawString(font, target_string, font_position + new Vector2(1, 0), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(1, 0), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position + new Vector2(0, 1), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(0, 1), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position, text_color, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f); // text itself

            sb.GraphicsDevice.ScissorRectangle = current; // reset old scissor rectangle

            sb.End(); // end current modified spritebatch

            draw_calls += 5;
            // reset with standard values
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend); // calls spritebatch back in with the static ui values
        }
        /// <summary>
        /// Main xna/monogame rendering function - draws a texture
        /// </summary>
        /// <param name="texture">sprite</param>
        /// <param name="position">where to draw on screen</param>
        /// <param name="crop">crop rectangle - masking</param>
        /// <param name="tint_color">what color to add over the texture</param>
        /// <param name="rotation_angle">rotation angle</param>
        /// <param name="sprite_origin">what part of the sprite is place over origin position</param>
        /// <param name="scale">zoom in/out</param>
        /// <param name="effects">flip effect</param>
        /// <param name="layerDepth">sorting against other sprites in the batch</param>
        public void xna_draw(Texture2D texture, Vector2 position, Nullable<Rectangle> crop, Color tint_color, float rotation_angle, Vector2 sprite_origin, float scale, SpriteEffects effects, float layerDepth)
        {
            sb.Draw(texture, position, crop, tint_color, rotation_angle, sprite_origin, scale, effects, layerDepth);
            draw_calls++;
        }
        /// <summary>
        /// Draw an outline around rectangle
        /// </summary>
        /// <param name="target">Rectangle to outline</param>
        /// <param name="outline">outline color</param>
        /// <param name="thickness">outline thickness in pixels</param>
        public void xna_draw_rectangle_outline(Rectangle target, Color outline, short thickness)
        {
            Rectangle line = new Rectangle(target.X, target.Y, target.Width, thickness); // default
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // top

            line.Y += (target.Height - thickness); // change origin to the bottom left corner 
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // bottom
            line.Y -= (target.Height - thickness); // change back
            line.Width = thickness; line.Height = target.Height; // change crop rectangle to go vertical from origin
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // left

            line.X += target.Width - thickness; // move origin to the right side
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // right
        }
        /// <summary>
        /// Draw a rectangular shape on screen
        /// </summary>
        /// <param name="target">Rectangle object</param>
        /// <param name="outline">outline color</param>
        /// <param name="outline_thickness">outline thickness</param>
        /// <param name="transparency">shape transparency</param>
        public void xna_draw_rectangle(Rectangle target, Color outline, short outline_thickness, float transparency)
        {
            // background
            Rectangle line = new Rectangle(target.X, target.Y, target.Width, target.Height); // default
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline * transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // top

            // outline
            line = new Rectangle(target.X, target.Y, target.Width, outline_thickness); // default
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // top

            line.Y += (target.Height - outline_thickness); // change origin to the bottom left corner 
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // bottom
            line.Y -= (target.Height - outline_thickness); // change back
            line.Width = outline_thickness; line.Height = target.Height; // change crop rectangle to go vertical from origin
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // left

            line.X += target.Width - outline_thickness; // move origin to the right side
            xna_draw(Engine.pixel, new Vector2(line.X, line.Y), line, outline, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // right
        }
        /// <summary>
        /// Get camera offset vector
        /// </summary>
        /// <returns>Vector2 numer of pixels that the camera moved from the original position</returns>
        public Vector2 get_camera_offset()
        {
            return camera_offset;
        }
        public void set_camera_offset(Vector2 position)
        {
            camera_offset = position;
        }
        public void toggle_light_pulses_flag()
        {
            light_pulses_flag = !light_pulses_flag;
        }
        /// <summary>
        /// Show the reach spheres of point lights
        /// </summary>
        /// <returns>true or false</returns>
        public bool show_light_pulses()
        {
            return light_pulses_flag;
        }
        /// <summary>
        /// Update camera offset
        /// </summary>
        /// <param name="drag_status">mouse middle button is held - true or false</param>
        public void move_camera(bool drag_status)
        {
            if(drag_status)
            {
                camera_offset.X -= (mouseState.X - OldMouseState.X);
                camera_offset.Y -= (mouseState.Y - OldMouseState.Y);
            }
        }
        /// <summary>
        /// Number of clouds and clouds that can produce rain particles 
        /// </summary>
        /// <returns></returns>
        public Int2 get_number_of_clouds_and_rain_clouds()
        {
            int rain = 0;
            foreach (Cloud c in clouds)
            {
                if (c.is_a_rain_cloud())
                {
                    rain++;
                }
            }

            return new Int2(clouds.Count, rain);
        }
        /// <summary>
        /// Converts rate per second to actual value to be applied based on milliseconds that passed
        /// </summary>
        /// <param name="initial">rate,e.g 50 per second</param>
        /// <param name="delta">number of milliseconds that passed</param>
        /// <returns>actual value based on milliseconds passed</returns>
        public static float convert_rate(float initial, float delta)
        {
            return (initial / 1000f) * delta;
        }
        /// <summary>
        /// Check neighbor cell by Vector2 address
        /// </summary>
        /// <param name="source">original cell</param>
        /// <param name="direction">which way</param>
        /// <param name="distance">number of cells to move</param>
        /// <param name="elevation_difference">1 = down, -1 = up</param>
        /// <returns></returns>
        public Vector2 neighbor_cell(Vector2 source, string direction, int distance, int elevation_difference = 0)
        {
            int offx = 0;
            int offy = 0;

            if(direction == "left")
            {
                offx = -distance;
                offy = elevation_difference;
            }
            else if(direction == "right")
            {
                offx = distance;
                offy = elevation_difference;
            }
            else if (direction == "top")
            {
                offy = -distance;
            }
            else if (direction == "bottom")
            {
                offy = distance;
            }
            else
            {
                // nothing
            }

            Vector2 new_vec = source + new Vector2(offx,offy);

            return new_vec;
        }
        /// <summary>
        /// Move camera based on arrows input
        /// </summary>
        /// <param name="input">up, down,left or right keys array (multiple allowed)</param>
        public void move_camera(Microsoft.Xna.Framework.Input.Keys[] input)
        {
            //if (editor.accepting_input()) // skip if text input is being received
                //return;

            if (!camera_moving)
            {
                camera_moving = true;
                camera_movement_start = current_game_millisecond;
            }

            if (((current_game_millisecond - camera_movement_start) >= update_step) || current_game_millisecond == camera_movement_start)
            {
                for (int j = 0; j < input.Length; j++)
                {
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.Up)
                        camera_offset.Y -= camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.Down)
                        camera_offset.Y += camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.Left)
                        camera_offset.X -= camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.Right)
                        camera_offset.X += camera_speed;
                }

                camera_movement_start = current_game_millisecond;
            }
        }
        /// <summary>
        /// Get viewport object - visible screen
        /// </summary>
        /// <returns>Viewport object</returns>
        public Viewport get_viewport()
        {
            return viewport;
        }
        /// <summary>
        /// Check if pressed key is a character
        /// </summary>
        /// <param name="key">input key on keyboard</param>
        /// <returns>true or false</returns>
        public static bool is_key_char(Microsoft.Xna.Framework.Input.Keys key)
        {
            return (key >= Microsoft.Xna.Framework.Input.Keys.A && key <= Microsoft.Xna.Framework.Input.Keys.Z);
        }
        /// <summary>
        /// check if input key is a digit
        /// </summary>
        /// <param name="key">input key on keyboard</param>
        /// <returns>true or false</returns>
        public static bool is_key_digit(Microsoft.Xna.Framework.Input.Keys key)
        {
            return (key >= Microsoft.Xna.Framework.Input.Keys.D0 && key <= Microsoft.Xna.Framework.Input.Keys.D9)
                || (key >= Microsoft.Xna.Framework.Input.Keys.NumPad0 && key <= Microsoft.Xna.Framework.Input.Keys.NumPad9);
        }
        /// <summary>
        /// Assist preferred keyboard repeat rate. Get time of the last processed input key
        /// </summary>
        /// <returns>timestamp in ms</returns>
        public long get_last_input_time()
        {
            return last_input_time;
        }

        /// <summary>
        ///  enqueue creation of this many lights
        /// </summary>
        /// <param name="n">number of lights to generate</param>
        public void order_light_generation(int n)
        {
            deferred_light_generation_order += n;
        }
        /// <summary>
        /// get current number of light waiting for generation
        /// </summary>
        /// <returns>number of lights</returns>
        public int get_deferred_lights_num()
        { 
            return deferred_light_generation_order;
        }
        /// <summary>
        /// Sets the last input timestamp
        /// </summary>
        /// <param name="value">new value in ms</param>
        public void set_last_input_time(long value)
        {
            last_input_time = value;
        }
        /// <summary>
        /// key - process this key + add results to the current focused input text box
        /// resets last_input_time to current value (if key was added to string) and add a char/valid input key 
        /// to string or do a backspace, otherwise shifts, caps locks, etc are ignored and checked later to transform acceptable key instead
        /// </summary>
        /// <param name="key">input key</param>
        public void process_key(Microsoft.Xna.Framework.Input.Keys key)
        {
            string result = "";
            // timer is reset + these get added or removed from current focused input
            // check character a-z, then apply caps lock and shift states (caps for letters, symbols for numbers)
            if (is_key_char(key))
            {
                if (Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock))
                {
                    if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)) // lower case
                    {
                        result = key.ToString().ToLower();
                    }
                    else // upper case
                    {
                        result = key.ToString();
                    }
                }
                else
                {
                    if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)) // upper case
                    {
                        result = key.ToString();
                    }
                    else // lower case
                    {
                        result = key.ToString().ToLower();
                    }
                }
                // finish up
                editor.get_current_input_target().add_text(result);
                last_input_time = current_game_millisecond;
                return; // return to caller
            }
            // check if backspace or space - then erase last character or add space
            else if (key == Microsoft.Xna.Framework.Input.Keys.Space)
            {
                result = " ";
                // finish up
                editor.get_current_input_target().add_text(result);
                last_input_time = current_game_millisecond;
                return; // return to caller
            }
            else if (key == Microsoft.Xna.Framework.Input.Keys.Back)
            {
                editor.get_current_input_target().erase_one_character();
                last_input_time = current_game_millisecond;
                return; // return to caller
            }
            // check number keys
            else if (is_key_digit(key))
            {
                if (!keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.D0)
                        result = "0";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D1)
                        result = "1";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D2)
                        result = "2";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D3)
                        result = "3";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D4)
                        result = "4";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D5)
                        result = "5";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D6)
                        result = "6";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D7)
                        result = "7";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D8)
                        result = "8";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D9)
                        result = "9";
                }
                else
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.D0)
                        result = ")";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D1)
                        result = "!";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D2)
                        result = "@";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D3)
                        result = "#";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D4)
                        result = "$";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D5)
                        result = "%";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D6)
                        result = "^";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D7)
                        result = "&";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D8)
                        result = "*";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.D9)
                        result = "(";
                }
                // finish up
                editor.get_current_input_target().add_text(result);
                last_input_time = current_game_millisecond;
                return; // return to caller
            }
            // check if `,./;'[]\=- or anything else , then apply shift state for second value of the key
            else
            {
                if (!keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.OemTilde)
                        result = "`";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemMinus)
                        result = "-";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPlus)
                        result = "=";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemQuotes)
                        result = "'";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPeriod)
                        result = ".";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets)
                        result = "[";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets)
                        result = "]";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemQuestion)
                        result = "/";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPipe)
                        result = "\\";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemComma)
                        result = ",";
                }
                else
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.OemTilde)
                        result = "~";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemMinus)
                        result = "_";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPlus)
                        result = "+";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemQuotes)
                        result = "\"";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPeriod)
                        result = ">";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets)
                        result = "{";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemPipe)
                        result = "|";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemQuestion)
                        result = "?";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets)
                        result = "}";
                    else if (key == Microsoft.Xna.Framework.Input.Keys.OemComma)
                        result = "<";
                }

                // finish up
                editor.get_current_input_target().add_text(result);
                last_input_time = current_game_millisecond;
                return; // return to caller
            }
            // timer is not reset + these are ignored
            //shift, caps lock, tab, ctrl etc - ignore and don't reset the timer
        }
        /// <summary>
        /// is world lighting on
        /// </summary>
        /// <returns>true or false</returns>
        public bool lighting_on()
        {
            return lighting_enabled;
        }
        /// <summary>
        /// Enable or disable light processing
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_lighting_state(bool value)
        {
            lighting_enabled = value;
        }
        /// <summary>
        /// Get fps tracker
        /// </summary>
        /// <returns>frames per second value</returns>
        public float get_fps()
        {
            return fps;
        }
        /// <summary>
        /// update current game second
        /// </summary>
        /// <param name="value">value - seconds</param>
        public void set_real_second(int value)
        {
            current_game_second = value;
        }
        /// <summary>
        /// Update real millisecond
        /// </summary>
        /// <param name="value">number of ms to add</param>
        public void add_real_millisecond(int value)
        {
            current_game_millisecond += value;
        }
        /// <summary>
        /// Get current value of milliseconds
        /// </summary>
        /// <returns>milliseconds value</returns>
        public static long get_current_game_millisecond()
        {
            return current_game_millisecond;
        }
        /// <summary>
        /// Mouse wheel value - prev
        /// </summary>
        /// <returns>wheel value</returns>
        public float get_prevWheelValue()
        {
            return prevWheelValue;
        }
        /// <summary>
        /// Mouse wheel value - current
        /// </summary>
        /// <returns>wheel value</returns>
        public float get_currWheelValue()
        {
            return currWheelValue;
        }
        /// <summary>
        /// Update previous mouse wheel value
        /// </summary>
        /// <param name="value">mouse wheel value</param>
        public void set_prevWheelValue(float value)
        {
            prevWheelValue = value;
        }
        /// <summary>
        /// Update current mouse wheel value
        /// </summary>
        /// <param name="value">mouse wheel value</param>
        public void set_currWheelValue(float value)
        {
            currWheelValue = value;
        }
        /// <summary>
        /// Reset number of calls to a draw function per frame
        /// </summary>
        public void clear_draw_calls_to_zero()
        {
            draw_calls = 0;
        }
        /// <summary>
        /// Number of calls to a draw function per frame
        /// </summary>
        /// <returns>current number of draw calls</returns>
        public int get_draw_calls()
        {
            return draw_calls;
        }
        public long get_draw_calls_total()
        {
            return draw_calls_total;
        }
        /// <summary>
        /// Formats number with K,M or B indicator for thousands, millions and billions
        /// </summary>
        /// <param name="num">converted number</param>
        /// <returns>string representation of a number</returns>
        public string number_to_KMB(int num)
        {
            if (num < 1000)
                return num.ToString();
            else if(num > 1000 && num < 1000000)
                return num.ToString("#,##0,K", CultureInfo.InvariantCulture);
            else if(num > 1000000 && num < 1000000000)
                return num.ToString("#,##0,0##,M", CultureInfo.InvariantCulture);
            else
                return num.ToString("#,##0,000,0##,B", CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Update total number of draw calls made to graphics card
        /// </summary>
        /// <param name="value">value to add - number of calls</param>
        public void add_draw_calls_total(int value)
        {
            draw_calls_total += value;
        }
        /// <summary>
        /// Update frame count by 1
        /// </summary>
        public void frame_plusone()
        {
            frames++;
        }
        /// <summary>
        /// update frames count by n
        /// </summary>
        /// <param name="n">number to add to frame count</param>
        public void frame_plus_n(int n)
        {
            frames += n;
        }
        /// <summary>
        /// return frame count
        /// </summary>
        /// <returns>number of frames since start. must not be equal to zero, since it's used in division - calculation of fps</returns>
        public int get_frame_count()
        {
            return frames == 0 ? 1 : frames; // never return 0
        }
        /// <summary>
        /// Get current wind speed value
        /// </summary>
        /// <returns>wind speed value in pixels per second</returns>
        public float get_wind_speed()
        {
            return wind.get_wind_speed();
        }
        /// <summary>
        /// Get expected wind speed - scheduled to be updated next
        /// </summary>
        /// <returns>future wind speed value in pixels per second</returns>
        public float get_wind_change()
        {
            return wind.get_wind_expected();
        }
        /// <summary>
        /// Time until wind speed update
        /// </summary>
        /// <returns>string value - for satistics</returns>
        public string get_wind_time_until_next_change()
        {
            return wind.time_until_next_change();
        }
        /// <summary>
        /// Check if pixel coordinated are within the screen. Not to be confused with a cell adress.
        /// </summary>
        /// <param name="pixel_position">position as Vector2, x and y pixel coordinates</param>
        /// <returns></returns>
        public bool is_within_visible_screen(Vector2 pixel_position) // not cell position
        {
            if(pixel_position.X > viewport.Width || pixel_position.X < 0)
            {
                return false;
            }

            if(pixel_position.Y > viewport.Height || pixel_position.Y < 0)
            { 
                return false;
            }

            return true;
        }
        /// <summary>
        /// Rain - get current chance
        /// </summary>
        /// <returns>int value 0-100</returns>
        public int get_rain_chance()
        {
            return rain_chance;
        }
        /// <summary>
        /// Update rain chance
        /// </summary>
        /// <param name="val">set rain chance 0-100</param>
        public void set_rain_chance(int val)
        {
            rain_chance = val > 100 ? 100 : val;
            last_rain_update = current_game_millisecond;
        }
        /// <summary>
        /// Update world clock rate
        /// </summary>
        /// <param name="value">set world clock rate - number of milliseconds that represent a minute</param>
        public void set_world_clock_rate(int value)
        {
            world_clock_rate = value;
        }
        /// <summary>
        /// Get world clock rate in milliseconds
        /// </summary>
        /// <returns>int value for milliseconds</returns>
        public int get_world_clock_rate()
        {
            return world_clock_rate;
        }
        /// <summary>
        /// Update world clock multiplier
        /// </summary>
        /// <param name="value">number of clock seconds to add per cycle</param>
        public void set_world_clock_multiplier(int value)
        {
            world_clock_multiplier = value;
        }
        /// <summary>
        /// Get world clock multiplier value
        /// </summary>
        /// <returns>update using the int value - number of seconds to add</returns>
        public int get_world_clock_multiplier()
        {
            return world_clock_multiplier;
        }
        /// <summary>
        /// Check if clock update is due
        /// </summary>
        /// <returns>number of milliseconds</returns>
        public long get_update_timer_split()
        {
            return current_game_millisecond - update_timer.checkpoint_value;
        }
        /// <summary>
        /// Set the millisecond value of the last clock update
        /// </summary>
        public void set_update_timer_split()
        {
            update_timer.checkpoint_value = current_game_millisecond;
        }
        /// <summary>
        /// Check if game is paused
        /// </summary>
        /// <returns>true or false</returns>
        public bool get_pause_status()
        {
            return paused;
        }
        /// <summary>
        /// Set game pause status
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_pause(bool value)
        {
            paused = value;
        }
        /// <summary>
        /// Equality of Vector2 objects
        /// </summary>
        /// <param name="a">1st vec</param>
        /// <param name="b">2nd vec</param>
        /// <returns>bool true or false</returns>
        public bool are_vectors_equal(Vector2 a, Vector2 b)
        {
            if (a.X == b.X && a.Y == b.Y)
                return true;

            return false;
        }
        /// <summary>
        /// Static: generate integer in a given range (boundaries included)
        /// </summary>
        /// <param name="low">low boundary</param>
        /// <param name="high">high boundary</param>
        /// <returns>a generated integer</returns>
        public static int generate_int_range(int low, int high)
        {
            if (high < low)
                throw new ArgumentException("high less than low");

            return rng.Next(low, high+1);
        }
        /// <summary>
        /// Get percentage of the way from min to max number. Updated: fixed bug with always returning 1f because min wasn't subtracted from current
        /// </summary>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximum value</param>
        /// <param name="current">current value</param>
        /// <returns>0f - 1f</returns>
        public float get_percentage_of_range(int min, int max, int current)
        {
            return (((float)current - (float)min) / ((float)max - (float)min)) > 1f ? 1f : (((float)current - (float)min) / ((float)max - (float)min));
        }
        /// <summary>
        /// Get percentage of the way from min to max number.
        /// </summary>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximum value</param>
        /// <param name="current">current value</param>
        /// <returns>0f - 1f</returns>
        public double get_percentage_of_range(long min, long max, long current)
        {
            if (current < min)
                return 0f;

            return (   ((double)current - (double)min) / ((double)max - (double)min)) > 1f ? 1f : (((double)current - (double)min)  / ((double)max - (double)min));
        }
        /// <summary>
        /// Static: Generate a float in a given range
        /// </summary>
        /// <param name="low">low boundary</param>
        /// <param name="high">high boundary</param>
        /// <returns>a generated float</returns>
        public static float generate_float_range(float low, float high)
        {
            if (high < low)
                throw new ArgumentException("high less than low");

            return (float)rng.NextDouble() * (high - low) + low;
        }
        /// <summary>
        /// Create a percentage calculator based on start - current - delay and duration of a timed event.
        /// Can be used for transparency or scale transformations.
        /// </summary>
        /// <param name="start">Timer start value - current_game_millisecond at the start of the process - should be saved in the caller</param>
        /// <param name="delay">Delay before some event can begin, e.g. begin fading in a banner animation</param>
        /// <param name="duration">Duration of the event - change of transparency from 0.0f to 1.0f</param>
        /// <returns>float value - current transparency based on timer. min = 0, max = 1f</returns>
        public static float fade_up(float start, float delay, float duration, float max_value = 1f)
        {
            // calculate transparency based on delay and fade time period
            float percentage = (current_game_millisecond - start - delay) / duration;

            float value = (percentage < 0.0f) // if percentage is less than 0
                ? 0.0f // assign a 0 to transparency
                :    // else
                (percentage > 1.0f ? 1.0f : percentage); // if more than 1 assign 1, otherwise assign value calculated before
            // return calculated result
            return value * max_value;
        }
        /// <summary>
        /// Calculates value for reducing transparency value or scale
        /// </summary>
        /// <param name="start">start millisecond</param>
        /// <param name="delay">delay for this effect to begin</param>
        /// <param name="duration">time it takes to go from 1 to 0 value</param>
        /// <param name="min_value">minimum value</param>
        /// <returns>current value based on time</returns>
        public static float fade_down(float start, float delay, float duration, float min_value = 0f)
        {
            // calculate transparency based on delay and fade time period
            float percentage = 1f - ((current_game_millisecond - start - delay) / duration); // reverse of the fade up

            float value = (percentage > 1.0f) // if percentage is more than 1
                ? 1.0f // assign a 1 to transparency
                :    // else
                (percentage < 0.0f ? 0.0f : percentage); // value shouldn't go below 0
            // return calculated result
            return value * min_value;
        }
        /// <summary>
        /// Calculates sine value for current game millisecond based on arbitrary duration
        /// This function is the same as fade pulse
        /// </summary>
        /// <param name="duration">Length of 1 cycle</param>
        /// <returns>current value in a given range based on time</returns>
        public static float fade_sine_wave_uneven(float duration)
        {
            duration *= 2; // multiply by 2 to compensate for sine wave negative half 
            float val = sine_wave(
                ((float)((int)current_game_millisecond % (int)duration) / duration) * (Math.PI * 2));
            // flip negative half of the sine wave, 
            // effectively generating two cycles in one duration, 
            // therefore to keep desired duration of 1 cycle - multiply duration value by 2
            return Math.Abs(val);
        }
        /// <summary>
        /// Creates a smooth curve in time
        /// </summary>
        /// <param name="duration">How long should 1 full cycle take</param>
        /// <param name="min">minimal value allowed</param>
        /// <param name="max">maximum value allowed</param>
        /// <param name="point">start at 0 or 1</param>
        /// <returns>current value based on time</returns>
        public static float fade_sine_wave_smooth(float duration, float min, float max, sinewave point = sinewave.zero)
        {
            // assign adjustment to start at 0 or 1
            float adjustment = (float)Math.PI / 4f; // 90 degrees
            if (point == sinewave.zero)
                adjustment = -adjustment;

            float x = (float)((int)current_game_millisecond % (int)duration) / duration;
            float val = (1 + Engine.sine_wave(2 * (x * (Math.PI) + adjustment))) / 2f;
            return min + (val * (max - min));
        }
        /// <summary>
        /// Calculates a sine value based on angle 0-2Pi
        /// </summary>
        /// <param name="angle">Radian value of an angle</param>
        /// <returns>calculated sine value givent he angle</returns>
        private static float sine_wave(double angle)
        {
            return (float)Math.Sin(angle);
        }
        /// <summary>
        /// Simulates a cyclic movement for sprite rotation
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>value based on time</returns>
        public static float cyclical_fade(float duration, float min, float max)
        {
            if (duration == 0)
                duration = 0.01f; // prevent division by zero

            float x = (float)((int)current_game_millisecond % (int)duration) / (float)duration;
            return min + (x * (max - min));
        }
        /// <summary>
        /// Simple Color negative
        /// </summary>
        /// <param name="source">Original color</param>
        /// <returns>Negative of the given color</returns>
        public Color negative_color(Color source)
        {
            return new Color(255 - source.R, 255 - source.G, 255 - source.B);
        }

        /// <summary>
        /// Update color - make it brighter or dimmer
        /// </summary>
        /// <param name="source">original color</param>
        /// <param name="factor">factor of change - under 1f - dimmer color, over 1f - brighter color</param>
        /// <returns>updated color</returns>
        public Color adjusted_color(Color source, float factor)
        {
            float R = source.R * factor;
            float G = source.G * factor;
            float B = source.B * factor;

            if (R > 255) { R = 255; }
            else if (R < 0) { R = 0; }

            if (G > 255) { G = 255; }
            else if (G < 0) { G = 0; }

            if (B > 255) { B = 255; }
            else if (B < 0) { B = 0; }

            source.R = (byte)R; source.G = (byte)G; source.B = (byte)B;
            return source;
        }
        /// <summary>
        /// Invert the color - contrast value
        /// </summary>
        /// <param name="source">Original color</param>
        /// <returns>updated color</returns>
        public Color inverted_color(Color source)
        {
            Vector3 hsl_converted = RGB2HSL(source);
            hsl_converted.X = Math.Abs(hsl_converted.X - 360f);

            Vector3 pre_inverted = HSL2RGB(hsl_converted);
            Color inverted = new Color(pre_inverted.X, pre_inverted.Y, pre_inverted.Z);

            return inverted;
        }
        /// <summary>
        /// Get color saturation value - to convert from rgb system to hsl color system
        /// </summary>
        /// <param name="source">original color</param>
        /// <returns>S component of the HSL - hue,saturation,lightness</returns>
        public float get_color_saturation(Color source)
        {
            float R1 = (float)source.R / 255.0f;
            float G1 = (float)source.G / 255.0f;
            float B1 = (float)source.B / 255.0f;

            float Cmax = max_of_three(R1, G1, B1); // maximum value and color
            float Cmin = min_of_three(R1, G1, B1); // minimum value and color
            float L = (Cmax + Cmin) / 2;

            float S = 0;
            if (L > 0.5f)
            {
                S = (Cmax - Cmin) / (2.0f - Cmax - Cmin);
            }
            else if (L < 0.5f)
            {
                S = (Cmax - Cmin) / (Cmax + Cmin);
            }
            return S;
        }
        /// <summary>
        /// Convert rgb color to HSL. Used the color formula at rapidtables.com as a base
        /// </summary>
        /// <param name="source">original color</param>
        /// <returns>returna  vector3 value of the color in hsl system</returns>
        public Vector3 RGB2HSL(Color source)
        {
            float R1 = (float)source.R / 255.0f;
            float G1 = (float)source.G / 255.0f;
            float B1 = (float)source.B / 255.0f;

            float Cmax = max_of_three(R1, G1, B1); // maximum value and color
            float Cmin = min_of_three(R1, G1, B1); // minimum value and color
            float L = (Cmax + Cmin) / 2;


            float D = Cmax - Cmin;
            // calculate color
            float H = 0;

            //If Luminance is smaller then 0.5, then Saturation = (max-min)/(max+min)
            //If Luminance is bigger then 0.5. then Saturation = ( max-min)/(2.0-max-min)
            float S = 0;
            if (L > 0.5f)
            {
                S = (Cmax - Cmin) / (2.0f - Cmax - Cmin);
            }
            else if (L < 0.5f)
            {
                S = (Cmax - Cmin) / (Cmax + Cmin);
            }
            //If Red is max, then Hue = (G-B)/(max-min)
            //If Green is max, then Hue = 2.0 + (B-R)/(max-min)
            //If Blue is max, then Hue = 4.0 + (R-G)/(max-min)
            if (R1 == Cmax)
            {
                H = (G1 - B1) / (Cmax - Cmin);
            }
            else if (G1 == Cmax)
            {
                H = 2.0f + ((B1 - R1) / (Cmax - Cmin));
            }
            else if (B1 == Cmax)
            {
                H = 4.0f + ((R1 - G1) / (Cmax - Cmin));
            }

            H *= 60f; // convert to degrees

            return new Vector3(H, S, L);
        }
        /// <summary>
        /// Convert HSL color back to rgb
        /// </summary>
        /// <param name="hsl">hsl format color</param>
        /// <returns>vector 3 in rgb system - r g and b 0-255</returns>
        public Vector3 HSL2RGB(Vector3 hsl)
        {
            float C = (1 - Math.Abs(2 * hsl.Z - 1)) * hsl.Y;
            float X = C * (1 - Math.Abs((hsl.X / 60.0f) % 6 - 1));
            float m = hsl.Z - C / 2;

            Vector3 result = new Vector3();

            if (hsl.X < 60f)
            {
                result.X = C; result.Y = X; result.Z = 0;
            }
            else if (hsl.X < 120f)
            {
                result.X = X; result.Y = C; result.Z = 0;
            }
            else if (hsl.X < 180f)
            {
                result.X = 0; result.Y = C; result.Z = X;
            }
            else if (hsl.X < 240f)
            {
                result.X = 0; result.Y = X; result.Z = C;
            }
            else if (hsl.X < 300f)
            {
                result.X = X; result.Y = 0; result.Z = C;
            }
            else if (hsl.X < 360f)
            {
                result.X = C; result.Y = 0; result.Z = X;
            }

            return result;
        }
        /// <summary>
        /// Rectangle object to vector2
        /// </summary>
        /// <param name="a">original Rectangle</param>
        /// <returns>vector representation</returns>
        public Vector2 rectangle_to_vector(Rectangle a)
        {
            return new Vector2(a.X, a.Y);
        }
        /// <summary>
        /// return the maximum of three elements float
        /// </summary>
        /// <param name="a">1st number</param>
        /// <param name="b">2nd number</param>
        /// <param name="c">3rd number</param>
        /// <returns>float value that is greater than the others</returns>
        public float max_of_three(float a, float b, float c)
        {
            float result = a >= c ? (a >= b ? a : b) : (c >= b ? c : b);
            return result;
        }
        /// <summary>
        /// return the maximum of three elements integer version
        /// </summary>
        /// <param name="a">1st number</param>
        /// <param name="b">2nd number</param>
        /// <param name="c">3rd number</param>
        /// <returns>int value that is greater than the others</returns>
        public int max_of_three(int a, int b, int c)
        {
            int result = a >= c ? (a >= b ? a : b) : (c >= b ? c : b);
            return result;
        }
        /// <summary>
        /// return the minimum of three elements float
        /// </summary>
        /// <param name="a">1st number</param>
        /// <param name="b">2nd number</param>
        /// <param name="c">3rd number</param>
        /// <returns>float value that is smaller than the others</returns>
        public float min_of_three(float a, float b, float c)
        {
            float result = a <= c ? (a <= b ? a : b) : (c <= b ? c : b);
            return result;
        }
        /// <summary>
        /// return the minimum of three elements integer version
        /// </summary>
        /// <param name="a">1st number</param>
        /// <param name="b">2nd number</param>
        /// <param name="c">3rd number</param>
        /// <returns>int value that is smaller than the others</returns>
        public int min_of_three(int a, int b, int c)
        {
            int result = a <= c ? (a <= b ? a : b) : (c <= b ? c : b);
            return result;
        }     
        /// <summary>
        /// Centered vector - in relation to a Rectangle
        /// for a given rectangle - calculate a top-left corner for displaying another rectangle centered
        /// this function returns a vector origin point to display a Texture2D - centered vertically,horizontally or both
        /// </summary>
        /// <param name="target">rectangle target</param>
        /// <param name="r">rectangle source</param>
        /// <param name="value">orientation of centering</param>
        /// <returns>vector2 position whenre the source rectangle should be placed in the target rectangle</returns>
        public Vector2 vector_centered(Rectangle target, Rectangle r, orientation value)
        {
            float x = ((target.Width - r.Width) / 2);
            float y = ((target.Height - r.Height) / 2);
            Vector2 origin = Vector2.Zero;
            // calculation switch
            switch (value)
            {
                case orientation.both:
                    origin = new Vector2(target.X + x, target.Y + y);
                    break;
                case orientation.horizontal:
                    origin = new Vector2(target.X + x, target.Y);
                    break;
                case orientation.vertical:
                    origin = new Vector2(target.X, target.Y);
                    break;
                case orientation.horizontal_bottom:
                    origin = new Vector2(target.X + x, target.Y + target.Height - r.Height);
                    break;
                default:
                    break;
            }
            // optimize pixel values (float values create blurred text)
            origin.X = (int)(origin.X);
            origin.Y = (int)(origin.Y);
            // result
            return origin;
        }
        /// <summary>
        /// Center one texture in another
        /// </summary>
        /// <param name="target"> Center in this rectangle</param>
        /// <param name="r">Center this sized item</param>
        /// <param name="value">how to center</param>
        /// <returns>vector value of the position</returns>
        public Vector2 vector_centered(Rectangle target, Vector2 r, orientation value)
        {
            float x = ((target.Width - r.X) / 2);
            float y = ((target.Height - r.Y) / 2);
            Vector2 origin = Vector2.Zero;
            // calculation switch
            switch (value)
            {
                case orientation.both:
                    origin = new Vector2(target.X + x, target.Y + y);
                    break;
                case orientation.horizontal:
                    origin = new Vector2(target.X + x, target.Y);
                    break;
                case orientation.vertical:
                    origin = new Vector2(target.X, target.Y);
                    break;
                case orientation.vertical_right:
                    origin = new Vector2(target.X + target.Width - r.X, target.Y + y);
                    break;
                case orientation.vertical_left:
                    origin = new Vector2(target.X, target.Y + y);
                    break;
                default:
                    break;
            }
            // optimize pixel values (float values create blurred text)
            origin.X = (int)(origin.X);
            origin.Y = (int)(origin.Y);
            // result
            return origin;
        }
        /// <summary>
        /// Get texture center for rotation effect and proper centered drawing
        /// </summary>
        /// <param name="source">Texture being used</param>
        /// <returns>center of this texture</returns>
        public Vector2 get_texture_center(Texture2D source)
        {
            return new Vector2(source.Width / 2, source.Height / 2);
        }
        /// <summary>
        /// Testing GUI loading from file
        /// </summary>
        /// <param name="engine">enginehelper</param>
        public void load_user_interface()
        {
            // create temporary lists for XML data
            List<ContainerData> temp_containers = new List<ContainerData>();
            List<ElementData> temp_elements = new List<ElementData>();
            List<UIData> ui_structure = new List<UIData>();
            // load xml
            try
            {
                using (FileStream stream = new FileStream("ui_containers.xml", FileMode.Open)) //open xml file, close??
                {
                    using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                    {
                        ContainerData[] ui_elements = IntermediateSerializer.Deserialize<ContainerData[]>(reader, null);
                        temp_containers = ui_elements.Cast<ContainerData>().ToList(); // load into the list
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // nothing yet
            }

            try
            {
                using (FileStream stream = new FileStream("ui_elements.xml", FileMode.Open)) //open xml file, close??
                {
                    using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                    {
                        ElementData[] elements = IntermediateSerializer.Deserialize<ElementData[]>(reader, null);
                        temp_elements = elements.Cast<ElementData>().ToList(); // load into the list
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // nothing yet
            }

            // create GUI and assign structure
            // NEW: only one editor object = load everything into it 
            GraphicInterface ui_reference = this.editor.GUI; // GUI contained inside editor which resides in this engine

            foreach (ContainerData data in temp_containers)
            {
                string id = (string)data.c_id;
                String name = data.name;
                context_type contexttype2 = (context_type)Enum.Parse(typeof(context_type), data.contexttype, true);
                Vector2 position = new Vector2(data.origin_x, data.origin_y);
                bool visible = Boolean.Parse(data.visible);


                Container temp = new Container(id, contexttype2, name, position, visible);
                // add container to list
                ui_reference.add_container(temp); // HERE: containers are loaded to GUI by default, maybe eliminate one assignment contexttype from XML objects
            }
            // at this point all containers have been loaded
            foreach (ElementData data in temp_elements)
            {
                string id = (string)data.id;
                string parent_id = (string)data.parent_id;
                string subcontext_id = (string)data.subcontext_id;
                type uitype = (type)Enum.Parse(typeof(type), data.ui_type, true);
                actions action = (actions)Enum.Parse(typeof(actions), data.action, true);
                confirm confirmation = (confirm)Enum.Parse(typeof(confirm), data.confirmation, true);
                Rectangle dimensions = new Rectangle(data.dimension_x, data.dimension_y, data.dimension_w, data.dimension_h);
                String iconname = data.icon_name;
                String label = data.label;
                String tooltip = data.tooltip;
                // determine what kind of element is created
                if (uitype == type.button || uitype == type.expandable_button)
                {
                    Button temp = new Button(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    // add UIElementBase to list
                    // an expandable button can also load a subcontext at this point based on the id provided in xml file
                    if (uitype == type.expandable_button && subcontext_id != "nosubcontext")
                    {
                        temp.assign_sub_context(ui_reference.find_container(subcontext_id));
                    }

                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.slider)
                {
                    Slider temp = new Slider(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    //ui_reference.add_element_temporary(temp);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.info_label)
                {
                    InfoLabel temp = new InfoLabel(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    //ui_reference.add_element_temporary(temp);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.value_button_binarychoice)
                {
                    SwitchButton<bool> temp = new SwitchButton<bool>(id, false, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.color_preview)
                {
                    ColorPreviewButton temp = new ColorPreviewButton(id, null, uitype, action, dimensions, label, tooltip);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.progress_bar)
                {
                    ProgressBar temp = new ProgressBar(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip, Color.White, true, true);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.progress_circle)
                {
                    ProgressCircle temp = new ProgressCircle(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip, Color.White, true, true);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.text_input)
                {
                    TextInput temp = new TextInput(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.text_area)
                {
                    TextArea temp = new TextArea(id, null, uitype, action, confirmation, dimensions, this.get_texture(iconname), label, tooltip);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
                else if (uitype == type.locker)
                {
                    UIlocker temp = new UIlocker(id, null, uitype, action, dimensions, label, tooltip);
                    ui_reference.find_container(parent_id).add_element(temp);
                }
            }
        }

        /// <summary>
        /// Collection of functions designed to detect intersections of two lines
        /// 1 = 1st line, 2 = 2nd line. p = beginning, q = end
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        int calculate_orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            int val = ((int)q.Y - (int)p.Y) * ((int)r.X - (int)q.X) - ((int)q.X - (int)p.X) * ((int)r.Y - (int)q.Y);

            if (val == 0) return 0;  // colinear

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }
        /// <summary>
        /// Helper function for interseciton calculator
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        bool onSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }
        /// <summary>
        /// Caluclates line intersection
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="q1"></param>
        /// <param name="p2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public bool line_intersection(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = calculate_orientation(p1, q1, p2);
            int o2 = calculate_orientation(p1, q1, q2);
            int o3 = calculate_orientation(p2, q2, p1);
            int o4 = calculate_orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }
        /// <summary>
        /// Generic xml serializer
        /// </summary>
        /// <typeparam name="T"> Type of object serialized</typeparam>
        /// <param name="source_object"> Object being serialized</param>
        public void serialize_data<T>(T source_object, string filename)
        {
            // binary serializer
            try
            {
                using (Stream stream = File.Open(filename, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    stream.Seek(0, SeekOrigin.Begin);
                    bin.Serialize(stream, source_object);

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Flush();
                }

            }
            catch (IOException)
            {
            }
        }
        /// <summary>
        /// test deserializer
        /// </summary>
        /// <typeparam name="T">type of expected objects</typeparam>
        /// <param name="filename">name of the binary file</param>
        public void deserialize_data<T>(string filename)
        {
            try
            {
                using (Stream stream = File.Open(filename, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();

                    if (typeof(T) == typeof(List<Container>))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        deserialized_containers = (List<Container>)bin.Deserialize(stream); //assign results to this list
                    }
                    else if (typeof(T) == typeof(Editor))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        deserialized_editor = (Editor)bin.Deserialize(stream); //assign results to this list
                    }
                }
            }
            catch (IOException)
            {
            }
        }
        /// <summary>
        /// Recreate point lights based on the save file
        /// </summary>
        public void deserialize_light_data()
        {
            foreach (WorldStruct w in get_world_list().worlds)
            {
                ArrayList light_map = new ArrayList();

                try
                {
                    // opening file
                    using (FileStream stream = new FileStream(w.world.get_world_name() + "_lights.xml", FileMode.OpenOrCreate)) //open xml file, close??
                    {
                        if (stream != null)
                            using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                            {

                                light_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            }
                    }
                    // building light objects
                    int total_count = light_map.Count;

                    for (int i = 0; i < total_count; i++)
                    {
                        Light_Data obj = (Light_Data)light_map[i];
                        if (light_map[i] != null)
                        {
                            PointLight temp = new PointLight(this, new Vector2(obj.cell_x, obj.cell_y), new Color(obj.tint_r, obj.tint_g, obj.tint_b), new Vector2(obj.origin_x, obj.origin_y), obj.radius, obj.intensity);
                            w.world.world_lights.Add(temp);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (NullReferenceException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (InvalidCastException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (Microsoft.Xna.Framework.Content.Pipeline.InvalidContentException e)
                {
                    Debug.WriteLine("[DEBUG INFO] Bad operation: " + e);
                }
            }
        }
        /// <summary>
        /// Deserializes water generator data
        /// </summary>
        public void deserialize_watergen_data()
        {
            foreach (WorldStruct w in get_world_list().worlds)
            {
                ArrayList water_map = new ArrayList();

                try
                {
                    // opening file
                    using (FileStream stream = new FileStream(w.world.get_world_name() + "_watergen.xml", FileMode.OpenOrCreate)) //open xml file, close??
                    {
                        if (stream != null)
                            using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                            {

                                water_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            }
                    }
                    // building water objects
                    int total_count = water_map.Count;

                    for (int i = 0; i < total_count; i++)
                    {
                        WaterData obj = (WaterData)water_map[i];
                        if (water_map[i] != null)
                        {
                            WaterGenerator temp = new WaterGenerator(new Vector2(obj.cell_x, obj.cell_y), new Vector2(obj.pos_x, obj.pos_y), obj.intensity);
                            w.world.wsources.Add(temp);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (NullReferenceException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (InvalidCastException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (Microsoft.Xna.Framework.Content.Pipeline.InvalidContentException e)
                {
                    Debug.WriteLine("[DEBUG INFO] Bad operation: " + e);
                }
            }
        }
        /// <summary>
        /// deserialize grass data
        /// </summary>
        public void deserialize_grass_data()
        {
            foreach (WorldStruct w in get_world_list().worlds)
            {
                ArrayList grass_map = new ArrayList();

                try
                {
                    // opening file
                    using (FileStream stream = new FileStream(w.world.get_world_name() + "_grass.xml", FileMode.Open)) //open xml file, close??
                    {
                        if (stream != null)
                            using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                            {

                                grass_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            }
                    }
                    // building Grass objects
                    int total_count = grass_map.Count;

                    for (int i = 0; i < total_count; i++)
                    {
                        Grass_Data obj = (Grass_Data)grass_map[i];
                        if (grass_map[i] != null)
                        {
                            Grass temp = new Grass(this, new Vector2(obj.origin_x, obj.origin_y));
                            temp.set_creation_time(Engine.get_current_game_millisecond());
                            w.world.grass_tiles.Add(temp);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (NullReferenceException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (InvalidCastException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (Microsoft.Xna.Framework.Content.Pipeline.InvalidContentException e)
                {
                    Debug.WriteLine("[DEBUG INFO] Bad operation: " + e);
                }
            }
        }
        /// <summary>
        /// Deserialize saved trees
        /// </summary>
        public void deserialize_tree_data()
        {
            foreach (WorldStruct w in get_world_list().worlds)
            {
                ArrayList tree_map = new ArrayList();

                try
                {
            // opening file
                    using (FileStream stream = new FileStream(w.world.get_world_name()+"_trees.xml", FileMode.Open)) //open xml file, close??
                    {
                        if (stream != null)
                            using (XmlReader reader = XmlReader.Create(stream)) // open file in xml reader
                            {

                                tree_map = IntermediateSerializer.Deserialize<ArrayList>(reader, null);
                            }
                    }
             // building Tree objects
                    int total_count = tree_map.Count;

                    for (int i = 0; i < total_count; i++)
                    {
                        Tree_Data obj = (Tree_Data)tree_map[i];
                        if (tree_map[i] != null)
                        {
                            Tree temp = null;
                            if(obj.name_modifier == "") // green tree
                            {
                                temp = new GreenTree(this, new Vector2(obj.origin_x, obj.origin_y),1);
                                temp.set_deserialization_values(obj.crown_variant, obj.base_variant, obj.max_trunks, Color.White, obj.tint_factor);
                            }
                            else if(obj.name_modifier == "palm")
                            {
                                temp = new PalmTree(this, new Vector2(obj.origin_x, obj.origin_y), 1);
                                temp.set_deserialization_values(obj.crown_variant, obj.base_variant, obj.max_trunks, Color.White, obj.tint_factor);
                            }

                            w.world.trees.Add(temp);
                        }
                    }
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (NullReferenceException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (InvalidCastException e)
                {
                    Debug.WriteLine("[DEBUG INFO] File doesn't exist: " + e);
                }
                catch (Microsoft.Xna.Framework.Content.Pipeline.InvalidContentException e)
                {
                    Debug.WriteLine("[DEBUG INFO] Bad operation: " + e);
                }
            }
        }
        /// <summary>
        /// Creates a comma delimited string of Rectangle
        /// </summary>
        /// <param name="source">Source Rectangle</param>
        /// <returns>comma delimited string</returns>
        public static string rectangle_to_delimited_string(Rectangle source)
        {
            string temp = source.X + "," + source.Y + "," + source.Width + "," + source.Height;
            return temp;
        }
        /// <summary>
        /// Creates an XNA Rectangle from a comma delimited surrogate string. Used for serialization of GUI position
        /// </summary>
        /// <param name="source">string containing rectangle parameters</param>
        /// <returns>Rectangle recreated from the string surrogate</returns>
        public static Rectangle delimited_string_to_rectangle(string source)
        {
            int x = 0; // position value placeholders
            int y = 0;
            int w = 0;
            int h = 0;

            string temp = ""; // a temp string to read in characters
            int order = 1; // which variable is being used 

            for (int i = 0; i < source.Length; i++) // read character by character 
            {
                if (source[i] == ',')
                {
                    // assign number and switch to the next variable
                    if (order == 1)
                    {
                        x = Int32.Parse(temp);
                    }
                    else if (order == 2)
                    {
                        y = Int32.Parse(temp);
                    }
                    else if (order == 3)
                    {
                        w = Int32.Parse(temp);
                    }
                    else
                    {
                        h = Int32.Parse(temp);
                    }
                    temp = ""; // reset temp string
                    order++;   // increase color component order
                }
                else
                {
                    temp = String.Concat(temp, source[i]);
                }
            }
            h = Int32.Parse(temp); // collects the last number

            return new Rectangle(x, y, w, h);
        }
        /// <summary>
        /// String reversal function
        /// </summary>
        /// <param name="original">original string</param>
        /// <returns>new string</returns>
        public static string reverse(string original)
        {
            // standard solution
            /*if (original == null || original.Length == 1)
                return (original);
            else
                return reverse(original.Substring(1,original.Length-1)) + original[0];
             */

            // 1 line solution
            return ((original == null || original.Length == 1) ? original : reverse(original.Substring(1, original.Length - 1)) + original[0]);
        }
        /// <summary>
        /// A one-line implementation of reverse number in C#
        /// </summary>
        /// <param name="num">original number</param>
        /// <returns>reversed number</returns>
        public static int reverse(int num)
        {
            return num < 10 ? num : (num % 10) * (int)Math.Pow(10, (int)Math.Log10(num)) + reverse(num / 10); // (int)log10 of 12345 is 4, 10 to power of 4 = 10000, 10000*5(last dig) = 50000, + reverse of remainder 1234
        }
        /// <summary>
        /// Check if the number is a power of two
        /// </summary>
        /// <param name="n">original number</param>
        /// <returns>true or false</returns>
        public static bool isPowerofTwo(int n)
        {
            // using one arithmetic operator
            //return (n & (n - 1)) == 0; // biotwise comparison of a number and it's previous number. for a power of two previous number has every bit different from the original number, so bitwise returns a 0
            // using no arithmetic operators
            while (((n & 1) == 0) && n > 1)
            {
                n = n >> 1; // shift 1 position
            }
            return (n == 1); // if number is 1 after everything then it was a power of two
        }
        /// <summary>
        /// get Game object
        /// </summary>
        /// <returns></returns>
        public Game1 getGame1()
        {
            return refgame;
        }
    }
    //=================================================================END OF ENGINE CLASS

    /// <summary>
    /// Integer version of the Vector2
    /// </summary>
    public struct Int2
    {
        int _X;
        int _Y;

        public Int2(int x, int y)
        {
            _X = x;
            _Y = y;
        }

        public int X
        {
            get { return _X; }
            set { _X = value; }
        }

        public int Y
        {
            get { return _Y; }
            set { _Y = value; }
        }

        public Vector2 convert_to_vector()
        {
            return new Vector2(_X, _Y);
        }
    }
}
