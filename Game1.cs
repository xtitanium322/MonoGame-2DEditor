using System;
using System.Diagnostics;                                                   // stop watch class + debug
using System.Collections;                                                   // stack
using System.Collections.Generic;                                           // list
using System.Linq;
using Microsoft.Xna.Framework;                                              
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Globalization;                                                 // various string formats
//----------------------------------
using System.Xml;                                                           // use xml files
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;  // xml serialization
using MyDataTypes;                                                          // data types structure for xml serialization
using System.IO;                                                            // filestream

/* 2015-2017 - aee. MonoGame UI project. Conversion from the XNA framework.
 * Purpose: Research possibility of creating a small video game engine. Work with Microsoft game creation library(es)*/
namespace EditorEngine
{
    /// <summary>
    /// This is the main class for a game app.
    /// </summary>
public class Game1 : Game // create a child class
{      
        public static GraphicsDeviceManager graphics = null;             // graphics device
        public SpriteBatch spriteBatch;                                  // spritebatch
        public Viewport viewport;                                        // window
        RenderTarget2D world_tile_buffer;                                // draw all world ui_elements to this memory
        RenderTarget2D world_light_buffer;                               // draw all world lighting to this memory
        RenderTarget2D world_ui_layer;
        RenderTarget2D world_ui_mask_layer;
        RenderTarget2D world_ui_mask_temp;                               // temporary storage for UI elements that had shader masking applied
        Effect fx_lightmap_shader, fx_blur_shader, fx_ui_masking_shader; // shader effect objects
        public static Texture2D pixel_texture;                           // assigned to Engine as static object (a single pixel texture used to fill larger shapes, rectangles and circles)
        public static SpriteFont small_font;
        public static SpriteFont large_font;
        public Engine engine;                                            // all the support functions 
        public List<screens> GameScreens = new List<screens>();          // contains all gamescreens
        public float world_percent_filled = 0f;                          // quick stat - fill percentage 
        public int screen_max_height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        public int screen_max_width  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;

        double render_time = 0.00;                                       // ms - entire draw cycle
        double update_time = 0.00;                                       // ms - update cycle
        double peak_render_time = 0.00;                                  // maximum drawing time
        double peak_update_time = 0.00;                                  // maximum update time
        int overtime_frames     = 0;                                     // number of frames that took longer than 1/60thg of a second
        int draw_call_previous  = 0;                                     // number of total draw calls last frame
        double target_frame_rate = 59.9;                                 // preferred frame rate

        // Time of Day
        public static DateTime thisDay = DateTime.Now;     
        // stopwatches for quick measurements of code performance                           
        Stopwatch render_stopwatch = new Stopwatch();
        Stopwatch update_stopwatch = new Stopwatch();
        GameWindow window = null;






// Game class begins
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;           // creates a hig definition graphics profile. helps with fx files quality(shaders)
            Content.RootDirectory = "Content";                          // where the assets are stored
            this.Window.Title = "engine";
            Window.AllowUserResizing = false;
            graphics.PreferredBackBufferHeight = 1080;                  // window height
            graphics.PreferredBackBufferWidth  = 1920;                  // window width

            window = this.Window;                                       // set a window reference
            window.AllowAltF4 = true;
            window.Position = new Point(0, 0);                          // move window to the top left corner
            window.IsBorderless = true;                                 // simulates fullscreen
            graphics.IsFullScreen = false;                               // start in fullscreen mode
            graphics.ApplyChanges();                                    // apply all graphics properties
        }

// Game Initialize Function - load assets
        protected override void Initialize()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            viewport = graphics.GraphicsDevice.Viewport;
            pixel_texture = new Texture2D(GraphicsDevice, 1, 1);          // create a single pixel texture
            Game1.pixel_texture.SetData<Color>(new[] { Color.White });    // initialize pixel texture for future color tint (make it white by default to support tint)
            engine = new Engine(ref spriteBatch,ref viewport, this);

            this.IsMouseVisible = false;
            this.IsFixedTimeStep = true;                                  // default = true - provides best results for update() in release version, target 30 fps for laptop, if false - unlimited fps
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / target_frame_rate);
            graphics.SynchronizeWithVerticalRetrace = false;              // default = true, V Sync to prevent screen tearing, drawback - lagging mouse

            // prevent game from crashing due to textures being not a power of 2 size
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;
            graphics.ApplyChanges();
            // create render targets
            world_tile_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_light_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_temp = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            // initialize screens (main game will be displayed/updated in game loop)
            GameScreens.Add(screens.screen_main);
            // initialize pause
            engine.set_pause(false);
            // base function call
            base.Initialize();
        }
 // Game Load Content Function
        protected override void LoadContent()
        {
            // loading fonts
            small_font = Content.Load<SpriteFont>("stats");
            large_font = Content.Load<SpriteFont>("largefont");
            engine.set_UI_font(small_font); // assign GUI font
            // display something while content is loading
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                GraphicsDevice.Clear(Color.Black);
                engine.xna_draw_outlined_text("LOADING", new Vector2(viewport.Width/2,viewport.Height/2), Vector2.Zero, Color.Purple, Color.Black, small_font);
            spriteBatch.End();
            // loading effects
            fx_lightmap_shader   = Content.Load<Effect>("lightmap_shader");
            fx_blur_shader       = Content.Load<Effect>("Blur");
            fx_ui_masking_shader = Content.Load<Effect>("ui_mask_shader");
            // adding Tile definitions
            // to add a new tile sprite - just load and name a texture in here. Everything else is created automatically
            Tile.add_tile(this.Content.Load<Texture2D>("testcell"), "test get_cell_address", 1);
            Tile.add_tile(this.Content.Load<Texture2D>("dirt"), "dirt", 2);
            Tile.add_tile(this.Content.Load<Texture2D>("stone_tile"), "stone", 3);
            Tile.add_tile(this.Content.Load<Texture2D>("asphalt"), "asphalt", 4);
            Tile.add_tile(this.Content.Load<Texture2D>("sand"), "sand", 5);
            Tile.add_tile(this.Content.Load<Texture2D>("lava"), "lava", 6);
            Tile.add_tile(this.Content.Load<Texture2D>("ice_block"), "ice", 7);
            Tile.add_tile(this.Content.Load<Texture2D>("sapphire"), "sapphire", 8);
            Tile.add_tile(this.Content.Load<Texture2D>("snow"), "snow", 9);
            Tile.add_tile(this.Content.Load<Texture2D>("iron"), "iron", 10);
            Tile.add_tile(this.Content.Load<Texture2D>("gold"), "gold", 11);
            // load icons into editor menus
            foreach(WorldStruct w in engine.get_world_list().worlds)
            {
                w.world.LoadContent(this.Content, engine);
            }
            engine.load_user_interface();
            engine.LoadContent(Content);
            engine.get_world_list().change_current(engine, "test world2");
        }
 // Game Unload Content Function
        protected override void UnloadContent()
        {// finalize maps when game is closed
            //serializing GUI
            try
            {
                engine.UnloadContent();
            }
            catch(System.Runtime.Serialization.SerializationException e)
            {
                Debug.WriteLine(e);
            }

            serialize_map_data(Content); // this function saves all world info into an xml file when the program exits

            Content.Unload();
        }
        // Game Update Function
        protected override void Update(GameTime gameTime)
        {
            update_stopwatch.Reset();
            update_stopwatch.Start(); // start counting - entire update should take 1-2 ms out of 16.67ms assigned per frame
            engine.refresh_keyboard_and_mouse();
            engine.set_currWheelValue(engine.get_current_mouse_state().ScrollWheelValue);
            engine.Update();

            // accept controls / input
            Controls();

            if (!engine.get_pause_status()) // update world clock if not paused
            {
                engine.add_real_millisecond(gameTime.ElapsedGameTime.Milliseconds);
                engine.set_real_second((int)(engine.get_current_game_millisecond() / 1000));

                engine.get_world_list().get_current().update_world(engine.get_clock(), engine);
                engine.get_world_list().get_current().update_light_spheres(engine.get_frame_count());
            }

            engine.save_previous_keyboard_and_mouse();
            engine.set_prevWheelValue(engine.get_currWheelValue());
            thisDay = DateTime.Now; // update current in-game time

// update section for engine textarea
            engine.engine_text_engine.clear_messages();
            string statistics_text = "";
            int draw_call_current = engine.get_draw_calls();
            //float seconds_elapsed = 0.0f;
            render_time = ((double)render_stopwatch.Elapsed.Ticks / 10000.0f);
            update_time = ((double)update_stopwatch.Elapsed.Ticks / 10000.0f);

            if ((render_time + update_time) > (1000.0 / target_frame_rate)) // frames taht took longer than 16.67 ms (60 fps rate)
                overtime_frames++;

            if (engine.get_frame_count() > 120) // allow for all the loading to be done before reading peak values
            {
                peak_render_time = render_time >= peak_render_time ? render_time : peak_render_time; // assign peak
                peak_update_time = update_time >= peak_update_time ? update_time : peak_update_time; // assign peak
            }

            statistics_text += "rendered[orange] objects:[orange] (per[orange] frame)[orange] /nl ---total: " + engine.number_to_KMB(engine.get_draw_calls()) + "[skyblue] /nl ---current: " + (draw_call_current - draw_call_previous) + "[0,255,40] /nl ";
            statistics_text += "fps:[orange] " + string.Format("{0:0.0}", engine.get_fps()) + "[skyblue] frame:[orange] " + engine.get_frame_count() + "[skyblue] /nl long[orange] frames[orange]: "
                + overtime_frames + "[red] " + " long[orange] frame[orange] %[orange]: " + string.Format("{0:0.00}", ((float)overtime_frames / (float)engine.get_frame_count()) * 100) + "[skyblue] % /nl ";

            float seconds_elapsed = ((float)engine.get_current_game_millisecond() / 1000.0f);

            render_time = ((double)render_stopwatch.Elapsed.Ticks / 10000.0f);
            update_time = ((double)update_stopwatch.Elapsed.Ticks / 10000.0f);

            WorldClock wc = engine.get_clock();
            statistics_text += "Seconds[orange] elapsed:[orange] " + string.Format("{0:0.0}", seconds_elapsed) + "[skyblue] /nl ";
            statistics_text += "Mouse[orange] position:[orange] " + engine.get_current_mouse_state().X + "[skyblue] : " + engine.get_current_mouse_state().Y + "[skyblue] /nl ";
            statistics_text += "Game[orange] clock[orange] " + " (" + wc.get_time_in_minutes() + ")[skyblue] " + " [" + wc.get_time_in_seconds() + "][skyblue] "
            + wc.get_time().X.ToString("00") + " : " + wc.get_time().Y.ToString("00") + " : "
            + wc.get_time().Z.ToString("00") + " " + wc.get_am_pm() + " /nl ";

            statistics_text += "render[orange] duration:[orange] " + string.Format("{0:00.00}", render_time) + "[skyblue] ms" + " (peak:[orange] " + string.Format("{0:00.00}", peak_render_time) + "[red])[orange] /nl ";
            statistics_text += "update[orange] duration:[orange] " + string.Format("{0:00.00}", update_time) + "[skyblue] ms" + " (peak:[orange] " + string.Format("{0:00.00}", peak_update_time) + "[red])[orange] /nl "; ;
            statistics_text += "{number of lights:}[orange] " + engine.get_world_list().get_current().get_number_of_lights() + " selected:[orange] " + engine.get_editor().get_selection_lights().Count + " /nl ";
            if (engine.get_world_list().get_current().in_edit_mode())
                statistics_text += "container:[green] " + engine.get_editor().GUI.get_hovered_container_id() +
                "[skyblue] /nl element:[green] " + engine.get_editor().GUI.get_hovered_element_id() + "[skyblue] /nl action:[green] " + engine.get_editor().GUI.get_hovered_element_action_text() + "[skyblue]";

            engine.engine_text_engine.add_message_element(engine, statistics_text); // create text 

            // save states
            draw_call_previous = draw_call_current;
            // end of the new statistics
            update_stopwatch.Stop();
            base.Update(gameTime);
        }
        // Game Draw function - target time <5ms per frame
        protected override void Draw(GameTime gameTime)
        {
            // turn these back on if game crashes
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;
            graphics.ApplyChanges();

            try
            {
                render_stopwatch.Reset();
                render_stopwatch.Start(); // start counting time
                // draw all visible screens in order of importance
                base.Draw(gameTime);
                create_ui_renders(spriteBatch); // create UI surfaces later displayed on screen

                if (GameScreens.Contains(screens.screen_main))
                    draw_main_game(gameTime, spriteBatch);
                //if (GameScreens.Contains(screens.screen_statistics))
                // draw_statistics(gameTime);    // draw stats if activated      
                // all supporting graphics
                draw_engine(gameTime);
                // finishing up         
                engine.add_draw_calls_total(engine.get_draw_calls());
                //engine.clear_draw_calls_to_zero();                   // reset draw call number for next frame ( only count total of each frame and not total overall)      
                engine.frame_plusone(); // increase frane count
                render_stopwatch.Stop();
            }
            catch (NullReferenceException e)
            {
                Debug.Write(e, ":error:");
            }

            base.Draw(gameTime);
        }
//====================================================================
// CREATING RENDER TARGET FOR UI - COMBINES ELEMENTS AND PIXEL MASKS IN A PROPER ORDER
        public void create_ui_renders(SpriteBatch spritebatch)
        {
            // UI render surfaces
            GraphicsDevice.SetRenderTarget(world_ui_mask_layer); // change render target and draw a masking layer to it
            GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                engine.get_world_list().get_current().draw_world_mask_layer(engine, spritebatch);
            spriteBatch.End();
            // UI render target - contains static positioned UI elements
            GraphicsDevice.SetRenderTarget(world_ui_layer); // change render target to draw user interface to a temporary surface
            GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                engine.get_world_list().get_current().draw_world_UI_static(engine, spritebatch); // DRAWS ALL NON CONTEXTS (static positioned elements)                 
            spriteBatch.End();
            // This section applies masking to UI elements contained in world_ui_layer (all elements with static positioning)
            GraphicsDevice.SetRenderTarget(world_ui_mask_temp);
                fx_ui_masking_shader.Parameters["maskTexture"].SetValue(world_ui_mask_layer);
            GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                fx_ui_masking_shader.CurrentTechnique.Passes[0].Apply();// apply masking effect on UI               
                engine.xna_draw(world_ui_layer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // draw UI layer after everything was masked                
            spriteBatch.End();
            // This section is independent from Shader effect - masking - game world needs to be fully visible unlike some of the UI
            GraphicsDevice.SetRenderTarget(world_ui_mask_temp); // setting the same render target
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                engine.get_world_list().get_current().draw_world_post_processing(engine, spritebatch);   
                engine.get_world_list().get_current().draw_world_UI_context_and_tooltips(engine, spritebatch); // DRAWS ALL CONTEXTS(dynamic position elements) (and tooltips) + selection matrix               
            spriteBatch.End();
        }
// MAIN GAME DRAWING SECTION
        public void draw_main_game(GameTime g, SpriteBatch spritebatch)
        {
            // parameters 
            if (engine.lighting_on())
                fx_lightmap_shader.Parameters["intensity"].SetValue(engine.get_world_list().get_current().get_ambient_light(engine.get_clock()));            // set ambient light intensity based on time of day 
        // RENDER TARGET SECTION
            // draw light sphere blurred
            GraphicsDevice.SetRenderTarget(world_light_buffer);
            GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                fx_blur_shader.CurrentTechnique.Passes[0].Apply();
                engine.get_world_list().get_current().world_draw_point_lights(engine, spritebatch);
            spriteBatch.End();
            // draw world map tiles
            GraphicsDevice.SetRenderTarget(world_tile_buffer);
            GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // Set deferred mode to draw everything in minimal gpu operations. also switch textures in draw function as few times as possible
                engine.get_world_list().get_current().draw_map(engine, spritebatch);
            spriteBatch.End();
        // MAIN SCREEN
            // add lights render target to shader
            GraphicsDevice.SetRenderTarget(null);
            fx_lightmap_shader.Parameters["lightsTexture"].SetValue(world_light_buffer);  // apply after a different rendertarget has been set on device                  

            GraphicsDevice.Clear(engine.get_world_list().get_current().get_sky_color() * engine.get_world_list().get_current().get_ambient_light(engine.get_clock())); // set sky color behind world
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            if (engine.lighting_on())
                fx_lightmap_shader.CurrentTechnique.Passes[0].Apply();// (effect) create a spotlight on the NEXT drawn element (world_tile_buffer)
                engine.xna_draw(world_tile_buffer, Vector2.Zero, null, Color.White, 0f, Vector2.Zero,
                    1f, // scaling
                    SpriteEffects.None, 1f); // draw world buffer
            spriteBatch.End();
            // draw world geometry lines
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            engine.get_world_list().get_current().draw_world_geometry(engine, spritebatch);// eliminates masking issues with shaders used on user interface layer or lines showing above UI. Also removes lighting effects from point lights if drawn here
            engine.get_world_list().get_current().world_draw_point_light_sources(engine); // actual light sources + info
            engine.get_world_list().get_current().world_draw_water_generators(engine);    // water generator markers 
            spriteBatch.End();
        // UI complete mask       
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                // bug fix: eliminated internal transparency and make interface transparency universal in order to make sure multi-component elements get evenly transparent
                engine.xna_draw(world_ui_mask_temp, Vector2.Zero, null, Color.White * engine.get_editor().get_interface_transparency(), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // draw UI layer after everything was masked
            spriteBatch.End();
        }
//====================================================================

// note: in following functions - do not do any clearing, because menu will not appear on it's own but as an overlay)
// ==================================================================================================================
// mouse pointer
        public void draw_engine(GameTime g)
        {
            GraphicsDevice.SetRenderTarget(null); // draw to screen 
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            engine.Draw(spriteBatch, engine.get_world_list().get_current());
            // format and display time
            Vector2 length = small_font.MeasureString("current time: " + thisDay.Hour.ToString("D2") + ":" + thisDay.Minute.ToString("D2") + " " + thisDay.Second.ToString("D2") + " " + thisDay.ToString("tt", System.Globalization.CultureInfo.InvariantCulture));
            engine.xna_draw_outlined_text(
                "current time: " + thisDay.Hour.ToString("D2") + ":" + thisDay.Minute.ToString("D2") + " " + thisDay.Second.ToString("D2") + " " + thisDay.ToString("tt", System.Globalization.CultureInfo.InvariantCulture),
                new Vector2(viewport.Width - (length.X + 5),5), Vector2.Zero, Color.White, Color.Black, small_font);
            spriteBatch.End();
        }
// save map data to the file
        // optimization
        public void serialize_map_data(ContentManager content)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            foreach (WorldStruct w in engine.get_world_list().worlds)
            {
                // create an array of mapdata to save
                //MapData[] tiles_save = new MapData[w.world.width * w.world.height];
                ArrayList tiles_save = new ArrayList();
                // save map data (new optimized version creates only as much space as needed)
                for (int i = 0; i < w.world.width; i++)
                {
                    for (int j = 0; j < w.world.height; j++)
                    {
                        if (!w.world.world_map[i, j].is_air())
                        {
                            int position = i * w.world.height + j; // calculates array position

                            MapData obj = new MapData();
                            obj.block_name = Tile.find_tile_name(w.world.world_map[i, j].tile_id);
                            obj.block_x = i + 1;
                            obj.block_y = j + 1;
                            obj.height = 1;
                            obj.width = 1;

                            // save the object
                            //tiles_save[position] = obj; // add MapData entry to the array
                            tiles_save.Add(obj);
                        }
                    }
                }
                // write xml
                using (XmlWriter writer = XmlWriter.Create(w.filename, settings))
                {
                    IntermediateSerializer.Serialize(writer, tiles_save, null); // write complete arraylist to the xml file
                }
            }
        }
// create a circle texture (intensity sphere of point lights)
        public static Texture2D createSmoothCircle(int radius, Color color, float intensity)
        {
            Texture2D texture = new Texture2D(graphics.GraphicsDevice, radius, radius); // creates an empty_texture square texture in memory
            Color[] colorData = new Color[radius * radius];                             // creates an array of colors corresponding to texture above
            float diam = radius / 2.0000000000f;   // diam = real radius
            float diamsq = diam * diam; // real radius squared

            for (int x = 0; x < radius; x++) // iterate through coordinates
            {
                for (int y = 0; y < radius; y++)
                {
                    int index = x * radius + y;
                    Vector2 pos = new Vector2(x - diam, y - diam); // adjust the origin to be in the center of the texture by subtracting half of x and y

                    if (pos.LengthSquared() <= diamsq) // calculate width of vector with a (0,0) origin
                    {
                        // calculating light power
                        float light_power = ((diamsq - pos.LengthSquared()) / diamsq) * intensity;
                        colorData[index] = color * light_power; // variable strength 
                    }
                    else
                    {
                        colorData[index] = Color.Transparent; // outside the circle
                    }
                }
            }
            // assign pixels
            texture.SetData(colorData);
            return texture;
        }
        // same as above but with no smoothing that would exist for lights
        public static Texture2D createOpaqueCircle(int radius, Color color, float intensity, bool outline_only = false)
        {
            Texture2D texture = new Texture2D(graphics.GraphicsDevice, radius, radius); // creates an empty_texture square texture in memory
            Color[] colorData = new Color[radius * radius];                             // creates an array of colors corresponding to texture above
            float diam = radius / 2.0000000000f;   // diam = real radius
            float diamsq = diam * diam; // real radius squared

            for (int x = 0; x < radius; x++) // iterate through coordinates
            {
                for (int y = 0; y < radius; y++)
                {
                    int index = x * radius + y;
                    Vector2 pos = new Vector2(x - diam, y - diam); // adjust the origin to be in the center of the texture by subtracting half of x and y

                    if (outline_only)
                    {
                        if (pos.LengthSquared() <= diamsq && pos.LengthSquared() >= ((diam - 1) * (diam - 1))) // calculate width of vector with a (0,0) origin
                        {
                            // calculating light power
                            float light_power = 0.9f;
                            colorData[index] = color * light_power; // variable strength 
                        }
                        else
                        {
                            colorData[index] = Color.Transparent; // outside the circle
                        }
                    }
                    else
                    {
                        if (pos.LengthSquared() <= diamsq) // calculate width of vector with a (0,0) origin
                        {
                            // calculating light power
                            float light_power = 0.9f;
                            colorData[index] = color * light_power; // variable strength 
                        }
                        else
                        {
                            colorData[index] = Color.Transparent; // outside the circle
                        }
                    }
                }
            }
            // assign pixels
            texture.SetData(colorData);
            return texture;
        }
        // half circle for composite percentage pie chart
        public static Texture2D createHalfCircle(int radius, Color color, float intensity)
        {
            Texture2D texture = new Texture2D(graphics.GraphicsDevice, radius, radius); // creates an empty_texture square texture in memory
            Color[] colorData = new Color[radius * radius];                             // creates an array of colors corresponding to texture above
            float diam = radius / 2.000f;   // diam = real radius
            float diamsq = diam * diam; // real radius squared

            for (int x = 0; x < radius; x++) // iterate through coordinates
            {
                for (int y = 0; y < radius; y++)
                {
                    int index = x * radius + y;
                    Vector2 pos = new Vector2(x - diam, y - diam); // adjust the origin to be in the center of the texture by subtracting half of x and y

                    if (pos.LengthSquared() <= diamsq && y <= radius/2) // calculate width of vector with a (0,0) origin
                    {
                        // calculating light power
                        float light_power = ((diamsq - pos.LengthSquared()) / diamsq) * intensity;/*- 0.375f;*/
                        colorData[index] = color * light_power; // variable strenght 
                    }
                    else
                    {
                        colorData[index] = Color.Transparent; // outside the circle
                    }
                }
            }
            // assign pixels
            texture.SetData(colorData);
            return texture;
        }
// create rectangles
        /// <summary>
        /// Creates a full rectangle
        /// </summary>
        /// <param name="bounds">rectangle dimensions</param>
        /// <param name="color">texture pixel color</param>
        /// <param name="intensity">transparency value</param>
        /// <returns>Texture2d of a created Rectangle</returns>
        public static Texture2D create_colored_rectangle(Rectangle bounds, Color color, float intensity)
        {
            Texture2D texture = new Texture2D(graphics.GraphicsDevice, bounds.Width, bounds.Height); // creates an empty_texture square texture in memory
            int width = (int)bounds.Width;
            int height = (int)bounds.Height;
            Color[] colorData = new Color[width*height];                    // creates an array of colors corresponding to texture above
            // start creating texture
            for (int x = 0; x < width*height; x++) // iterate through coordinates
            {
              colorData[x] = color * intensity; 
            }
            // assign pixels
            texture.SetData(colorData);
            return texture;
        }


        /// <summary>
        /// Creates a hollow rectangular texture2d in a given size, color and border outline_thickness
        /// </summary>
        /// <param name="bounds">rectangle dimensions</param>
        /// <param name="color">texture pixel color</param>
        /// <param name="intensity">transparency value</param>
        /// <param name="border_width">border width in pixels</param>
        /// <returns>Texture2d of a created hollow Rectangle</returns>
        public static Texture2D create_colored_hollow_rectangle(Rectangle bounds, Color color, float intensity, int border_width)
        {
            Texture2D texture = new Texture2D(graphics.GraphicsDevice, bounds.Width, bounds.Height); // creates an empty_texture square texture in memory
            int width = (int)bounds.Width;
            int height = (int)bounds.Height;
            Color[] colorData = new Color[width * height];                    // creates an array of colors corresponding to texture above
            // start creating texture
            for (int x = 0; x < width; x++) // iterate through coordinates
            {
                for (int y = 0; y < height; y++)
                {
                    if ( // if x and y fall within acceptable range - change color data to desried color and intensity
                    (
                        (x < border_width) || x >= (width - border_width))
                        || ((y < border_width) || (y >= height - border_width))
                    )
                        colorData[y * width + x] = color * intensity; // set color data - convert x and y to linear coordinate
                }
            }
            // assign pixels
            texture.SetData(colorData);
            return texture;
        }
// Keyboard and Mouse controls - move to Engine and optimize + add controller support
        public void Controls()
        {
            // ---------------------------------------------------THIS IS an UPDATE() iteration BASED VERSION - some keys might be missed due to lower framerates. fast typing is not supported by this method
            // text input section       
            // setting keyboard states
            if (engine.get_editor().accepting_input()) // make sure there is input ready to receive
            {
                Microsoft.Xna.Framework.Input.Keys[] prev_input = engine.get_previous_keyboard_state().GetPressedKeys(); // (no need to save current in prev - already done in keyboard states (if nothing is pressed - prev_input.Length == 0)
                Microsoft.Xna.Framework.Input.Keys[] input = engine.get_current_keyboard_state().GetPressedKeys();  // Keys[] = array of keys being pressed right now

                foreach (Microsoft.Xna.Framework.Input.Keys current in input)
                {
                    if (current != Microsoft.Xna.Framework.Input.Keys.None) // eliminate the key
                    {
                        // if this key is contained in the previous input - ignore it or add after 120 milliseconds passed
                        // if it's still in the input due to two keys being presed at the same time in transition - timeout will eliminate repeating character
                        if (prev_input.Contains(current)) // if previous was Keys.None it automatically send an otherwise doubled character to the lower bracket where it will be processed immediately, otherwise held key will be processed on timeout
                        {// backspace timeout will fall under this section as well

                            if (!engine.input_repeated) // input hasn't been repeated once
                            {
                                if (engine.get_current_game_millisecond() - engine.get_last_input_time() >= 500) // long delay (for new keys)
                                {
                                    engine.process_key(current);
                                    engine.input_repeated = true; // set flag to use shorter delay now 
                                }
                            }
                            else // input has been repeated - set shorter delay
                            {
                                if (engine.get_current_game_millisecond() - engine.get_last_input_time() >= 15) // short delay (for repeated keys)
                                {
                                    engine.process_key(current);
                                }
                            }
                        }
                        // new key in this input = process it right away
                        else /*if (!prev_input.Contains(current))*/
                        {
                            engine.process_key(current);
                            engine.input_repeated = false; // means there was a new key processed - reset long repeat delay
                        }
                    }
                }
            }
            //end text input---------------------------------------------------------
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    // combination left shift + escape closes the game
                    if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                        this.Exit(); // close the program

                    if (engine.get_world_list().get_current().in_edit_mode())
                    {
                        engine.get_world_list().get_current().toggle_edit_mode(); // close the edit windows
                        engine.get_editor().unfocus_inputs(); // remove focus from inputs if editor gui is closed
                        engine.get_editor().hide_all_contexts();
                    }
                    else
                    {
                    }
                }
                // pressing TAB will enter/leave map edit mode
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Tab) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Tab))
                {
                    engine.get_world_list().get_current().toggle_edit_mode();
                    engine.get_editor().unfocus_inputs(); // remove focus from inputs if editor gui is closed
                    engine.get_editor().hide_all_contexts();

                    if (GameScreens.Contains(screens.screen_menu))
                        GameScreens.Remove(screens.screen_menu);

                }
                // pressing ENTER will send a command to editor
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter))
                {
                    engine.get_world_list().get_current().execute_command(command.enter_key, engine, this);
                }
                //F1 - statistics overlay
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1)) // prevents sticking
                {
                    engine.get_editor().GUI.find_container("CONTAINER_STATISTICS_TEXT_AREA").set_visibility("toggle");
                }
                //F2 - menu overlay - will eventually be moved to ESC key, exit will be one of the options on the menu
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F2) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F2)) // prevents sticking
                {
                    if (engine.get_world_list().get_current().worldname == "test world")
                        engine.get_world_list().change_current(engine, "test world2");
                    else
                        engine.get_world_list().change_current(engine, "test world");
                }
                //F3 - show or hide all light pulses circles in edit mode
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F3) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F3)) // prevents sticking
                {
                    engine.toggle_light_pulses_flag();
                }

                if (!engine.get_editor().accepting_input() && engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.P) && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.P))
                {
                    engine.set_pause(!engine.get_pause_status());
                }
                // mouse wheel scrolling 
                if (engine.get_currWheelValue() != engine.get_prevWheelValue())
                {
                    // actions
                    if (engine.get_currWheelValue() > engine.get_prevWheelValue())
                    {
                        // up
                        engine.get_world_list().get_current().execute_command(command.mouse_scroll_down, engine, this);
                    }
                    else if (engine.get_prevWheelValue() > engine.get_currWheelValue())
                    {
                        // down
                        engine.get_world_list().get_current().execute_command(command.mouse_scroll_up, engine, this);
                    }
                }
                if(engine.get_current_mouse_state().MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    //engine.get_world_list().get_current().execute_command(command.middle_hold, engine, this);
                    engine.move_camera(true);
                }
                // left mouse click
                if
                ((engine.get_previous_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released
                && engine.get_current_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed))
                {
                    // mouse button clicked
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        if (!engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                            engine.get_world_list().get_current().execute_command(command.left_click, engine, this);
                        else if(engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
                            engine.get_world_list().get_current().execute_command(command.ctrl_plus_click, engine, this);
                    }
                    else if (GameScreens.Contains(screens.screen_menu) && !engine.get_world_list().get_current().in_edit_mode())
                    {
                        //engine.interface_UI("game menu editor_command",this);
                    }
                }
                // left mouse hold
                if (engine.get_previous_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && engine.get_current_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        engine.get_world_list().get_current().execute_command(command.left_hold, engine, this);
                    }
                }
                // left mouse released
                if (engine.get_previous_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && engine.get_current_mouse_state().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
                {
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        engine.get_world_list().get_current().execute_command(command.left_release, engine, this);
                    }
                }
                // right mouse click
                if
                ((engine.get_previous_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released
                && engine.get_current_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed))
                {
                    // nothing yet
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        engine.get_world_list().get_current().execute_command(command.right_click, engine, this);
                    }
                }
                // right mouse hold
                if
                ((engine.get_previous_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && engine.get_current_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed))
                {
                    // nothing yet
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        engine.get_world_list().get_current().execute_command(command.right_hold, engine, this);
                    }
                }
                // right mouse released
                if
                ((engine.get_previous_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && engine.get_current_mouse_state().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released))
                {
                    // nothing yet
                    if (engine.get_world_list().get_current().in_edit_mode() && !GameScreens.Contains(screens.screen_menu))
                    {
                        engine.get_world_list().get_current().execute_command(command.right_release, engine, this);
                    }
                }
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Delete)
                && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Delete))
                {
                    if (engine.get_world_list().get_current().in_edit_mode())
                        engine.get_editor().editor_command(engine, command.delete_key);
                }
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Insert)
                 && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Insert))
                {
                    if (engine.get_world_list().get_current().in_edit_mode())
                        engine.get_editor().editor_command(engine, command.insert_key);
                }
                // alt + key combinations
                if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
                {
                    // send command to Editor based on other keys held, world edit state
                    /*if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Q)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Q))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_q);
                    }*/
                    if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.C)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.C))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_c);
                    }
                    /*else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.E)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.E))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_e);
                    }*/
                    else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Z)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Z))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.destroy_water_gen);
                    }
                    else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.X)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.X))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.destroy_lights);
                    }
                    /*else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_1);
                    }
                    else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_2);
                    }
                    else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_3);
                    }
                    else if (engine.get_current_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4)
                    && !engine.get_previous_keyboard_state().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4))
                    {
                        if (engine.get_world_list().get_current().in_edit_mode())
                            engine.get_editor().editor_command(engine, command.alt_4);
                    }*/
                }

                KeyboardState ks = engine.get_current_keyboard_state();
                engine.move_camera(ks.GetPressedKeys());
        }// end controls function

        public void update_resolution(int width, int height)
        {
            // don't change the resolution if screen width doesn't allow it
            if(width > screen_max_width || height > screen_max_height)
            {
                engine.get_editor().GUI.get_text_engine().add_message_element(engine, "resolution "+width.ToString()+"[255,0,0] : "+height.ToString()+"[255,0,0] not applicable to this monitor");
                return;
            }
            // update window default starting position
            window.Position = new Point(0, 0);
            // update resolution
            graphics.PreferredBackBufferHeight = height;  // window height
            graphics.PreferredBackBufferWidth = width; // window width                        
            graphics.ApplyChanges();

            // update other variables
            window = this.Window;
            viewport = graphics.GraphicsDevice.Viewport;
            engine.refresh_viewport(ref viewport);

            // update render targets
            world_tile_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_light_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_temp = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        }

        public void make_fullscreen(bool mode)
        {
            this.Window.Position = new Point(0, 0);

            if (mode)
                this.Window.IsBorderless = true;
            else
                this.Window.IsBorderless = false;

            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.ApplyChanges();

            // update other variables
            window = this.Window;
            viewport = graphics.GraphicsDevice.Viewport;
            engine.refresh_viewport(ref viewport);

            // update render targets
            world_tile_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_light_buffer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_layer = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            world_ui_mask_temp = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        }
    }
}
