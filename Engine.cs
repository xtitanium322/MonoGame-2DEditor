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

using System.Xml;                                                           // use xml files
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;  // xml serialization
using System.Runtime.Serialization.Formatters.Binary;
using MyDataTypes;                                                          // data types structure
using System.IO;
using System.Windows.Forms;
/* Class contains all engine variables/values used in the game */
namespace beta_windows
{
    public class Engine
    {
        public static Texture2D pixel = Game1.pixel_texture;               // standard white pixel used to draw most primitives
        public static Rectangle standard20 = new Rectangle(0, 0, 20, 20);  // standard size 20 px rectangle
        private Game1 refgame;

        public WorldClock clock;
        private int world_clock_rate;                                      // number of frames to Update world clock
        private int world_clock_multiplier;                                // number of seconds added per Update cycle
        private bool paused;                                                // is the game paused?
        private bool lighting_enabled;                                      // calculate and display lighting in the game world
        private int frames;                                                // total number of frames drawn/updated
        private float fps;                                                   // frames per second
        private int draw_calls = 0;                                        // number of times xna draw has been called since last Update
        private long draw_calls_total = 0;                                  // total number of xna draw calls since game launch
        private float prevWheelValue, currWheelValue;                        // mousewheel values
        private int current_game_second;                                   // real time since start of the game in : seconds
        private long current_game_millisecond;                              // real time since start of the game in : milliseconds
        private bool camera_moving;
        private long camera_movement_start;                                 // calculate when camera movement started
        private Vector2 camera_offset;                                        // controls the camera
        private int camera_speed = 20;                                        // for simple camera movement - number of pixels to move (important: match tile_size for smooth line movement)
        private int update_step = 8;                                          // limit number of milliseconds it takes to move camera again
        Random rng;
        SpriteBatch sb;                                                       // spritebatch object
        Viewport viewport;                                                    // viewport object
        private List<texture_element> all_textures;                           // list of frequently used named textures
        private SpriteFont UIfont;                                            // font assigned to GUI       
        private ValueTimer<long> update_timer;
        // keyboard and mouse
        private KeyboardState keyboardState;
        private KeyboardState OldKeyboardState;
        private MouseState mouseState;
        private MouseState OldMouseState;
        private WorldCollection world_list;
        private Editor editor;                      // Editor class - for adding/deleting/selecting/tweaking ui_elements,lights etc. (originally in Editor - now will exists as a universal engine feature - pass current world as a paramater)
        public float grid_transparency_value;      // used to display cell placement grid
        public int gridcolor_r;
        public int gridcolor_g;
        public int gridcolor_b;
        private Color grid_color;                  // used to draw editor grid

        private long last_input_time = 0; // for keyboard input repeating rate
        public bool input_repeated = false; // flag set to initiate a repeat

        private List<Container> deserialized_containers = null; // deserialized container information goes here - previous state re-used
        private Editor deserialized_editor = null;

        private struct ValueTimer<T>                                          // timer checking for changes in certain values other than time itself
        {
            public long millisecond_checkpoint; // last checkpoint
            public T checkpoint_value;          // value being tracked by this timer
            public int checkpoint_rate;         // number of milliseconds before assigning new checkpoint value
            public ValueTimer(T checkpoint_value, int rate)
                : this()
            {
                this.checkpoint_value = checkpoint_value;
                checkpoint_rate = rate;
                millisecond_checkpoint = 0;
            }
        }

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

        ValueTimer<int> FPStracker;                                           // instance of timer tracking framerate changes
        // constructor
        public Engine()
        {

        }
        public Engine(ref SpriteBatch sb, ref Viewport vp, Game1 g)
        {
            refgame = g;
            world_clock_rate = 20;         // larger number = slower time
            world_clock_multiplier = 15;  // number of seconds added per Update. more = faster
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
            all_textures = new List<texture_element>();
            //UIfont = statistics_font;
            FPStracker = new ValueTimer<int>(frames, 250); // check for the value of frame every X milliseconds
            camera_moving = false;
            update_timer = new ValueTimer<long>();
            // assign keyboard and mouse states
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
        }
        // load content
        public void LoadContent(ContentManager content)
        {
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_addmode"), "icon-addmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_deletemode"), "icon-deletemode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_selectmode"), "icon-selectmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("icon_lightsmode"), "icon-lightsmode"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("slider_line"), "item-sliderline"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("active_indicator"), "icon-active-indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("expansion_indicator"), "icon-expansion-indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("custom_200x30"), "200x30_background1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("custom_240x30"), "240x30_background1"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("overlay_deleted_cell"), "deleted_indicator"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("slider_progress"), "slider_progress"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("ghost_scroller_top"), "ghost_scroller_top"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("ghost_scroller_bottom"), "ghost_scroller_bottom"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse"), "mouse_default")); // default mouse
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse_hand"), "mouse_hand")); // special mouse (hover)
            all_textures.Add(new texture_element(content.Load<Texture2D>("mouse_move_indicator"), "mouse_move_indicator")); // special mouse (GUI move mode)
            all_textures.Add(new texture_element(content.Load<Texture2D>("progress250x40"), "progress_mask"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("progress250x40border"), "progress_border"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lock_icon"), "editor_icon_locked"));
            all_textures.Add(new texture_element(content.Load<Texture2D>("lock_open_icon"), "editor_icon_unlocked"));

            world_list.add_world("world1.xml", new World(this, content, "test world", 600, 120));
            world_list.add_world("world2.xml", new World(this, content, "test world2", 100, 50));
            world_list.load_tiles(this); // load all the xml files containing world tile info

            editor.LoadContent(content, this); // loads default user interface into the editor
            // deserializes previously saved user interface
                deserialize_data<List<Container>>("user_interface.bin"); // user interface  container list
                deserialize_data<Editor>("editor.bin");                  // selected editor variables
            // update necessary values using deserialized list of user interface containers and their inner elements 
                editor.seed_interface_with_serialized_data(deserialized_containers);
                editor.seed_interface_with_color_data(deserialized_editor);
        }
        public void UnloadContent()
        {   // finalize objects when game is closed
            List<Container> a = get_editor().GUI.get_all_containers();

            serialize_data<List<Container>>(a, "user_interface.bin");
            serialize_data<Editor>(editor, "editor.bin");
        }
        // engine class main Update function
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

            // update GUI option connected values
            grid_transparency_value = editor.GUI.get_slider_value(actions.update_slider_grid_transparency);
            gridcolor_r = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_red);
            gridcolor_g = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_green);
            gridcolor_b = (int)editor.GUI.get_slider_value(actions.update_slider_grid_color_blue);
            grid_color = new Color(gridcolor_r, gridcolor_g, gridcolor_b);

            editor.Update(this);
        }
        // update viewport
        public void refresh_viewport(ref Viewport v)
        {
            viewport = v;
        }
        // engine class Draw function
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
                else // non-hovered
                {
                    xna_draw(this.get_texture("mouse_default"), get_mouse_vector(), null, negative_color(editor.get_interface_color()), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                }
            }
            // move mode
            else
            {
                xna_draw(this.get_texture("mouse_move_indicator"), get_mouse_vector(), null, negative_color(editor.get_interface_color()), 0f, get_texture_center(this.get_texture("mouse_move_indicator")), 1f, SpriteEffects.None, 1f);
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
        public WorldCollection get_world_list()
        {
            return world_list;
        }

        public World get_current_world()
        {
            return world_list.get_current();
        }
        public WorldClock get_clock()
        {
            return clock;
        }
        public void set_UI_font(SpriteFont font)
        {
            UIfont = font;
        }
        public SpriteFont get_UI_font()
        {
            return UIfont;
        }
        public Color get_grid_color()
        {
            return grid_color;
        }
        public bool is_camera_moving()
        {
            return camera_moving;
        }
        public void set_camera_movement_flag(bool value)
        {
            camera_moving = value;
        }

        public void refresh_keyboard_and_mouse()
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
        }

        public void save_previous_keyboard_and_mouse()
        {
            OldKeyboardState = keyboardState;
            OldMouseState = mouseState;
        }

        public MouseState get_current_mouse_state()
        {
            return mouseState;
        }
        public KeyboardState get_current_keyboard_state()
        {
            return keyboardState;
        }
        public MouseState get_previous_mouse_state()
        {
            return OldMouseState;
        }
        public KeyboardState get_previous_keyboard_state()
        {
            return OldKeyboardState;
        }
        // get a texture stored 
        public Texture2D get_texture(String name)
        {
            foreach (texture_element t in all_textures)
            {
                if (t.get_name() == name)
                    return t.get_texture();
            }
            return null;
        }
        // return current position of a mouse as a 1 cell collision rectangle
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
        // simple rendering functions
        public void xna_draw_text(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color c, SpriteFont font)
        {
            sb.DrawString(font, target_string, font_position, c, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            draw_calls++;
        }
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
        public void xna_draw_outlined_text(String target_string, Vector2 font_position, Vector2 font_rotation_point, Color text_color, Color outline, SpriteFont font)
        {
            sb.DrawString(font, target_string, font_position + new Vector2(1, 0), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(1, 0), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position + new Vector2(0, 1), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position - new Vector2(0, 1), outline, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f);
            sb.DrawString(font, target_string, font_position, text_color, 0.0f, font_rotation_point, 1.00f, SpriteEffects.None, 1.0f); // text itself

            draw_calls += 4;
        }

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

        public void xna_draw(Texture2D texture, Vector2 position, Nullable<Rectangle> crop, Color tint_color, float rotation_angle, Vector2 sprite_origin, float scale, SpriteEffects effects, float layerDepth)
        {
            sb.Draw(texture, position, crop, tint_color, rotation_angle, sprite_origin, scale, effects, layerDepth);
            draw_calls++;
        }

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
        // camera 
        public Vector2 get_camera_offset()
        {
            return camera_offset;
        }
        public void set_camera_offset(Vector2 position)
        {
            camera_offset = position;
        }

        public void move_camera(Microsoft.Xna.Framework.Input.Keys[] input)
        {
            if (editor.accepting_input()) // skip if text input is being received
                return;

            if (!camera_moving)
            {
                camera_moving = true;
                camera_movement_start = current_game_millisecond;
            }

            if (((current_game_millisecond - camera_movement_start) >= update_step) || current_game_millisecond == camera_movement_start)
            {
                for (int j = 0; j < input.Length; j++)
                {
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.W)
                        camera_offset.Y -= camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.S)
                        camera_offset.Y += camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.A)
                        camera_offset.X -= camera_speed;
                    if (input[j] == Microsoft.Xna.Framework.Input.Keys.D)
                        camera_offset.X += camera_speed;
                }

                camera_movement_start = current_game_millisecond;
            }
        }

        public Viewport get_viewport()
        {
            return viewport;
        }
        // check keys
        public static bool is_key_char(Microsoft.Xna.Framework.Input.Keys key)
        {
            return (key >= Microsoft.Xna.Framework.Input.Keys.A && key <= Microsoft.Xna.Framework.Input.Keys.Z);
        }

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
        /// Sets the last input timestamp
        /// </summary>
        /// <param name="value">new value in ms</param>
        public void set_last_input_time(long value)
        {
            last_input_time = value;
        }
        // key - process this key + add results to the current focused input text box
        // resets last_input_time to current value (if key was added to string) and add a char/valid input key to string or do a backspace, otherwise shifts, caps locks, etc are ignored and checked later to transform acceptable key instead
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
        //end check keys
        public bool lighting_on()
        {
            return lighting_enabled;
        }
        public void set_lighting_state(bool value)
        {
            lighting_enabled = value;
        }
        // get_fps tracker
        public float get_fps()
        {
            return fps;
        }
        // real time/timer
        public void set_real_second(int value)
        {
            current_game_second = value;
        }
        public void add_real_millisecond(int value)
        {
            current_game_millisecond += value;
        }
        public long get_current_game_millisecond()
        {
            return current_game_millisecond;
        }
        // Mouse wheel
        public float get_prevWheelValue()
        {
            return prevWheelValue;
        }
        public float get_currWheelValue()
        {
            return currWheelValue;
        }
        public void set_prevWheelValue(float value)
        {
            prevWheelValue = value;
        }
        public void set_currWheelValue(float value)
        {
            currWheelValue = value;
        }
        // Draw call statistics
        public void clear_draw_calls_to_zero()
        {
            draw_calls = 0;
        }
        public int get_draw_calls()
        {
            return draw_calls;
        }
        public long get_draw_calls_total()
        {
            return draw_calls_total;
        }
        /*public void add_draw_calls(int value)
        {
            draw_calls += value;
        }*/
        public void add_draw_calls_total(int value)
        {
            draw_calls_total += value;
        }
        // framerate statistics
        public void frame_plusone()
        {
            frames++;
        }
        public void frame_plus_n(int n)
        {
            frames += n;
        }
        public int get_frame_count()
        {
            return frames;
        }
        // world clock rates/values
        public void set_world_clock_rate(int value)
        {
            world_clock_rate = value;
        }
        public int get_world_clock_rate()
        {
            return world_clock_rate;
        }
        public void set_world_clock_multiplier(int value)
        {
            world_clock_multiplier = value;
        }
        public int get_world_clock_multiplier()
        {
            return world_clock_multiplier;
        }

        public long get_update_timer_split()
        {
            return current_game_millisecond - update_timer.checkpoint_value;
        }
        public void set_update_timer_split()
        {
            update_timer.checkpoint_value = current_game_millisecond;
        }
        // game pause value
        public bool get_pause_status()
        {
            return paused;
        }
        public void set_pause(bool value)
        {
            paused = value;
        }
        // GUI Interface
        public void interface_gameplay(string command, World w, Engine engine)
        {
            // no game action for main menu GUI yet
        }

        /* generate some random numbers*/
        public int generate_int_range(int low, int high)
        {
            if (high < low)
                throw new ArgumentException("high less than low");

            return rng.Next(low, high);
        }
        public float get_percentage_of_range(int min, int max, int current)
        {
            return ((float)current / ((float)max - (float)min));
        }
        public float generate_float_range(float low, float high)
        {
            if (high < low)
                throw new ArgumentException("high less than low");

            return (float)rng.NextDouble() * (high - low) + low;
        }
        /// <summary>
        /// Create a percentage calculator based on start - current - delay and duration of a timed event.
        /// Can be used for transparency or scale transformations.
        /// </summary>
        /// <param name="start">Timer start value</param>
        /// <param name="delay">Delay before some event can begin, e.g. begin fading in a banner animation</param>
        /// <param name="duration">Duration of the event - change of transparency from 0.0f to 1.0f</param>
        /// <returns>float value - current transparency based on timer. min = 0, max = 1f</returns>
        public float fade_up(float start, float delay, float duration, float max_value = 1f)
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
        /// <param name="min_value"></param>
        /// <returns></returns>
        public float fade_down(float start, float delay, float duration, float min_value = 0f)
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
        /// <returns></returns>
        public float fade_sine_wave_uneven(float duration)
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
        /// <returns></returns>
        public float fade_sine_wave_smooth(float duration, float min, float max, sinewave point = sinewave.zero)
        {
            // assign adjustment to start at 0 or 1
            float adjustment = (float)Math.PI / 4f;
            if (point == sinewave.zero)
                adjustment = -adjustment;

            float x = (float)((int)current_game_millisecond % (int)duration) / duration;
            float val = (1 + sine_wave(2 * (x * (Math.PI) + adjustment))) / 2f;
            return min + (val * (max - min));
        }
        /// <summary>
        /// Calculates a sine value based on angle 0-2Pi
        /// </summary>
        /// <param name="angle">Radian value of an angle</param>
        /// <returns></returns>
        private float sine_wave(double angle)
        {
            return (float)Math.Sin(angle);
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

        // use this function as a unit test starting point on Monday 4/25/2016
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
        public Color inverted_color(Color source)
        {
            Vector3 hsl_converted = RGB2HSL(source);
            hsl_converted.X = Math.Abs(hsl_converted.X - 360f);

            Vector3 pre_inverted = HSL2RGB(hsl_converted);
            Color inverted = new Color(pre_inverted.X, pre_inverted.Y, pre_inverted.Z);

            return inverted;
        }

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
        public Vector2 rectangle_to_vector(Rectangle a)
        {
            return new Vector2(a.X, a.Y);
        }
        // return the maximum of three elements float
        public float max_of_three(float a, float b, float c)
        {
            float result = a >= c ? (a >= b ? a : b) : (c >= b ? c : b);
            return result;
        }
        // return the maximum of three elements int
        public int max_of_three(int a, int b, int c)
        {
            int result = a >= c ? (a >= b ? a : b) : (c >= b ? c : b);
            return result;
        }
        // return the minimum of three elements float
        public float min_of_three(float a, float b, float c)
        {
            float result = a <= c ? (a <= b ? a : b) : (c <= b ? c : b);
            return result;
        }
        // return the minimum of three elements int
        public int min_of_three(int a, int b, int c)
        {
            int result = a <= c ? (a <= b ? a : b) : (c <= b ? c : b);
            return result;
        }
        // for a given rectangle - calculate a top-left corner for displaying another rectangle centered
        // this function returns a vector origin point to display a Texture2D - centered vertically,horizontally or both
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
        /// <returns></returns>
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
        // Collection of functions designed to detect intersections of two lines
        // 1 = 1st line, 2 = 2nd line. p = beginning, q = end
        int calculate_orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            int val = ((int)q.Y - (int)p.Y) * ((int)r.X - (int)q.X) - ((int)q.X - (int)p.X) * ((int)r.Y - (int)q.Y);

            if (val == 0) return 0;  // colinear

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }
        bool onSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }
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

        // camera that chases character at a constant rate of full distance to character in X ms. 
        public void update_camera(Rectangle character_bounds, Vector2 character_position, Vector2 camera_offset)// character bounds, character current origin, current camera offset from 0,0 
        {
        }

        /// <summary>
        /// Generic xml serializer
        /// </summary>
        /// <typeparam name="T"> Type of object serialized</typeparam>
        /// <param name="source_object"> Object being serialized</param>
        public void serialize_data<T>(T source_object, string filename)
        {
            /* XmlWriterSettings settings = new XmlWriterSettings();
             settings.Indent = true;

             System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(source_object.GetType());
            
             using (XmlWriter writer = XmlWriter.Create("test_serialization.xml", settings))
             {
                 x.Serialize(writer, source_object, null); // write object to the xml file
             }*/

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
        /// Creates an XNA Rectangle from a comma delimited surrogate string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Rectangle delimited_string_to_rectangle(string source)
        {
            int x = 0;// color value placeholders
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

            return new Rectangle(x,y,w,h);
        }
        // testing string reversal
        public static string reverse(string original)
        {
            // standard solution
            /*if (original == null || original.Length == 1)
                return (original);
            else
                return reverse(original.Substring(1,original.Length-1)) + original[0];*/

            //1 line solution
            return ((original == null || original.Length == 1) ? original : reverse(original.Substring(1,original.Length-1)) + original[0]);
        }

        public static int reverse(int num)
        {
            return num < 10 ? num : (num % 10) * (int)Math.Pow(10, (int)Math.Log10(num)) + reverse(num/10); // (int)log10 of 12345 is 4, 10 to power of 4 = 10000, 10000*5(last dig) = 50000, + reverse of remainder 1234
        }

        public static bool isPowerofTwo(int n)
        {
            // using one arithmetic operator
                //return (n & (n - 1)) == 0; // biotwise comparison of a number and it's previous number. for a power of two previous number has every bit different from the original number, so bitwise returns a 0
            // using no arithmetic operators
                while(((n&1)==0) && n > 1)
                {
                    n = n>>1; // shift 1 position
                }
                return (n == 1); // if number is 1 after everything then it was a power of two
        }

        public Game1 getGame1()
        {
            return refgame;
        }
    }
    //=================================================================END OF ENGINE CLASS
    /* Vector2 replacement for stored_elements which only require int values */
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
