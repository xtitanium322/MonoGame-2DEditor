using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
/*
 * Old element IDs
 * 1000 = MODE_BUTTON_ADDMODE
 * 1039 = CURRENT_EDITOR_CELL_CHANGER_SUBCONTEXT
 * 1042 = CURRENT_EDITOR_CELL_CHANGER_BUTTON
 * 1029 = INFOLABEL_BRUSH_RADIUS
 * 1050 = PROGRESS_BAR_WORLDFILL_PERCENTAGE
 * 1037 = SWITCH_CLOCK_PAUSE_RESTART
 * 1031 = SLIDER_BRUSHSIZE
 * 1041 = PREVIEW_SELECTIONCOLOR
 * 1043 = INFOLABEL_CURRENT_EDIT_TILE
 * 1044 = PREVIEW_EDITORGRID
 */
namespace beta_windows
{
    [Serializable()]
    public struct color_theme
    {
        public string id; // identifier for this theme
        [NonSerialized]
        public Color interface_color;
        [NonSerialized]
        public Color text_color;
        public string interface_color_surrogate;
        public float interface_transparency;

        public color_theme(string id, Color interface_color, Color text_color, float transparency)
        {
            this.id = id;
            this.interface_color = interface_color;
            this.text_color = text_color;
            interface_transparency = transparency;
            interface_color_surrogate = TextEngine.color_to_delimited_string(interface_color); // creating a standardized string to be serialized in binary process
        }
        /// <summary>
        /// Sets up an XNA Color object from a surrogate string. Color itself is not serializeable.
        /// </summary>
        public void deserialize_color_string()
        {
            interface_color = TextEngine.delimited_string_to_color(interface_color_surrogate);
        }

        public Color get_color()
        {
            return interface_color;
        }
    }

    [Serializable()]
    public class Editor
    {
        [NonSerialized]
        private modes editor_mode;                                      // editor mode
        [NonSerialized]
        private tools editor_tools;                                     // editor tool
        [NonSerialized]
        private int submode_brush_radius;                               // this value tracks the size of submode brush used in combination with mode.add or mode.delete`. Ranges from 0 to 10
        [NonSerialized]
        private short edit_tile_id;                                     // which tile contexttype is currently highlighted
        [NonSerialized]
        private List<Vector2> selection_matrix;                         // list of all selected cells
        [NonSerialized]
        private List<Vector2> line_matrix;                              // list of all line cells
        [NonSerialized]
        public GraphicInterface GUI;                                    // create a User Interface object in this host class  
        [NonSerialized]
        private Vector2? selection_start_cell, selection_end_cell;      // start/end cells in selection
        [NonSerialized]
        public Vector2 engine_offset = new Vector2(0, 0);                // placeholder vector for offsetting
        // line tool start/end points. If start cell exists - moving mouse makes program update current line status. 
        // Right click cancels start cell. Turn off context clicking while line start cell exists
        [NonSerialized]
        public Vector2 line_start_cell, line_end_cell;

        // color parameters of the GUI
        [NonSerialized]
        private List<color_theme> themes = new List<color_theme>();
        private float sel_transparency;
        [NonSerialized]
        private Color selection_color;
        private string selection_color_surrogate;
        private color_theme current_theme;

        [NonSerialized]
        private actions? action;                                        // current editor action
        [NonSerialized]
        public bool gui_move_mode;                                      // testing gui element movement
        [NonSerialized]
        public bool locked;                                             // locked?
        [NonSerialized]
        private bool overwrite_cells;                                   // true = generate tile even if one exists in the cell, false = ignore existing ui_elements
        [NonSerialized]
        private bool editor_actions_locked;                             // true - clicking on world map won't change anything, false - tools are unlocked
        // current text input functionality
        [NonSerialized]
        private TextInput current_focused;                              // since input can only be accepted by one target at a time - create a placeholder 
        [NonSerialized]
        private const int input_delay = 300;                            // amount of millisecond allowed before next keystroke is added to input string of current focused input
        [NonSerialized]
        private const int backspace_delay = 100;
        [NonSerialized]
        private long last_input_timestamp = 0;                          // when was the last input received
        [NonSerialized]
        private long last_backspace_timestamp = 0;                      // when was the last character erased
        // constructors
        public Editor()
        {

        }
        public Editor(Engine engine)
        {
            //parent_world = w;
            editor_mode = modes.add;
            editor_tools = tools.radius;
            edit_tile_id = 2;
            submode_brush_radius = 0; // default = 0 cell radius - 1 cell currently pointed 
            selection_start_cell = selection_end_cell = null;      // no cells selected by default
            line_start_cell = new Vector2(-1, -1);
            line_end_cell = new Vector2();
            selection_matrix = new List<Vector2>(1024); // initialize selection matrix
            line_matrix = new List<Vector2>(512);
            GUI = new GraphicInterface(engine); // initialize user interface
            //sel_transparency = 0.5f;
            locked = false;
            gui_move_mode = false; // UI can't be repositioned
            overwrite_cells = false; // default = false
            editor_actions_locked = false;
            selection_color = new Color(0, 175, 250); 
            selection_color_surrogate = TextEngine.color_to_delimited_string(selection_color);
            // loading themes (contains alist of system defined colors)
            themes.Add(new color_theme("Dark", Color.Black, Color.White, 1f));
            themes.Add(new color_theme("White", Color.White, Color.Black, 1f));
            themes.Add(new color_theme("Deep Sky Blue", Color.DeepSkyBlue, Color.White, 1f));
            themes.Add(new color_theme("Orange Red", Color.OrangeRed, Color.White, 1f));
            themes.Add(new color_theme("Dark Red", Color.DarkRed, Color.White, 1f));
            themes.Add(new color_theme("Yellow Green", Color.YellowGreen, Color.Black, 1f));
            themes.Add(new color_theme("Golden", Color.Gold, Color.White, 1f));
            themes.Add(new color_theme("Gray", Color.DimGray, Color.White, 1f));
            themes.Add(new color_theme("Antique White", Color.AntiqueWhite, Color.Black, 1f));
            themes.Add(new color_theme("Crimson", Color.Crimson, Color.White, 1f));
            themes.Add(new color_theme("Acid Lime", Color.Lime, Color.White, 1f));
            themes.Add(new color_theme("Vibrant Blue", new Color(0, 128, 255), Color.White, 1f));
            themes.Add(new color_theme("Dark Blue", new Color(0, 30, 200), Color.White, 1f));
            current_theme = themes[0];
        }
        // functions  
        /// <summary>
        /// Loads content textures and creates an overall menu for editor GUI
        /// </summary>
        /// <param name="content">content object which will load .png textures</param>
        /// <param name="engine">Engine object reference</param>
        /// <param name="w">World object in which the editor object was created</param>
        public void LoadContent(ContentManager content, Engine engine/*, World w*/)
        {
            // Dynamically create a list of cell designs to select from in editor and assign this subcontext to expandable button (1039)
            Container temp = new Container("CONTAINER_EDITOR_TILE_CHANGER", context_type.expansion, "cell selection", Vector2.Zero, false);
            string temp_id = "BUILDING_CELLS_"; // base of the id given to these buttons
            int counter = 0;

            foreach (tile_struct t in Tile.get_list_of_tiles())
            {
                IDButton<short> temp_cell = new IDButton<short>(temp_id + counter.ToString(), temp, type.id_button, actions.change_cell_design, new Rectangle(0, counter * 30, 220, 30), t.get_name(), "");
                temp_cell.enable_button(t.get_id(), t.get_tile_icon_clip());
                temp.add_element(temp_cell);
                counter++;
            }
            // Dynamically add theme selection to context menu
            Container theme_container = GUI.find_container("CONTAINER_INTERFACE_COLOR_OPTIONS");// subcontext name for interface subcontext
            temp_id = "COLOR_THEMES_";
            counter = 0;

            foreach (color_theme t in themes)
            {
                IDButton<string> temp_cell = new IDButton<string>(temp_id + counter.ToString(), theme_container, type.id_button, actions.switch_theme, new Rectangle(0, counter * 20, 300, 20), t.id, "");
                temp_cell.enable_button(t.id, null); // assign theme id to button
                theme_container.add_element(temp_cell);
                counter++;
            }
            // assign subcontext tile selection to right places
            if (GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_SUBCONTEXT").get_type() == type.expandable_button
                && GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON").get_type() == type.expandable_button)
            {
                ((Button)GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_SUBCONTEXT")).assign_sub_context(temp);
                ((Button)GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON")).assign_sub_context(temp);
            }
            GUI.add_container(temp);
            // NOTE: ui elements are loaded from xml file during initialization
            load_initial_slider_values(engine);
            GUI.create_UI_backgrounds();
            // load any custom items after GUI has been set up
            GUI.load_custom_element_background("MODE_BUTTON_ADDMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_DELETEMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_SELECTMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_LIGHTSMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_container_background("CONTAINER_BRUSH_SIZE_OPTIONS", engine.get_texture("240x30_background1")); GUI.set_element_label_positioning("INFOLABEL_BRUSH_RADIUS", orientation.vertical_right);
            GUI.load_custom_container_background("CONTAINER_CURRENT_EDIT_CELL_PREVIEW", engine.get_texture("240x30_background1"));
            ((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).enable_button(false, engine.get_texture("editor_icon_locked"), engine.get_texture("editor_icon_unlocked"));
            GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON").set_icon(Tile.get_tile_struct(edit_tile_id).get_tile_icon_clip()); // 1042 - id of tile preview element
            // enable scrollbars if needed
            GUI.find_container("CONTAINER_EDITOR_TILE_CHANGER").enable_scrollbar(16, 4);

            // set world fill progress bar mask
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_mask(engine);
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_border(engine);
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_progress_color(Color.CornflowerBlue);

            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_X")).set_progress_color(Color.OrangeRed); // changing progress color
            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_Y")).set_progress_color(Color.MediumVioletRed); // changing progress color
            ((TextInput)GUI.find_element("TEXTINPUT_SYSTEM")).set_input_target("TEXTAREA_SYSTEM"); // sets source for text area

            // set boundaries for text area
            GUI.get_text_engine().set_target(((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_rectangle(), ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_origin());

        }
        /// testing slider value seeding
        public void load_initial_slider_values(Engine e)
        {
            // do not assign slider actions to anything but a slider otherwise this function will fail on invalid cast . Add try-catch block
            ((Slider)GUI.find_unit(actions.update_selection_transparency)).set_slider_values(sel_transparency, 0.25f, 0.75f, 2);
            ((Slider)GUI.find_unit(actions.update_selection_color_red)).set_slider_values(selection_color.R, 0, 255);
            ((Slider)GUI.find_unit(actions.update_selection_color_green)).set_slider_values(selection_color.G, 0, 255);
            ((Slider)GUI.find_unit(actions.update_selection_color_blue)).set_slider_values(selection_color.B, 0, 255);

            ((Slider)GUI.find_unit(actions.update_slider_grid_transparency)).set_slider_values(e.grid_transparency_value, 0.05f, 1.0f, 2);
            ((Slider)GUI.find_unit(actions.update_slider_grid_color_red)).set_slider_values(e.gridcolor_r, 0, 255);
            ((Slider)GUI.find_unit(actions.update_slider_grid_color_green)).set_slider_values(e.gridcolor_g, 0, 255);
            ((Slider)GUI.find_unit(actions.update_slider_grid_color_blue)).set_slider_values(e.gridcolor_b, 0, 255);

            ((Slider)GUI.find_unit(actions.update_brush_size)).set_slider_values(submode_brush_radius, 0, 10);

            // SET SWITCH BUTTON VALUES
            ((SwitchButton<bool>)GUI.find_element("SWITCH_CLOCK_PAUSE_RESTART")).set_value(true); // clock is inactive by default
            ((SwitchButton<bool>)GUI.find_element("SWITCH_ENABLE_WORLD_LIGHTING")).set_value(true); // lighting is active by default
            //((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).enable_button(false, e.get_texture("editor_icon_locked"), e.get_texture("editor_icon_unlocked")); // locks ui movement by default
            // SET progress bars
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_element_values(0, 100, (int)e.get_current_world().get_percent_filled() * 100);
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).update((int)(e.get_current_world().get_percent_filled() * 100.0f));
            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_X")).set_element_values(0, 100, (int)((e.get_mouse_vector().X / e.get_viewport().Bounds.Width) * 100f));
            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_Y")).set_element_values(0, 100, (int)((e.get_mouse_vector().Y / e.get_viewport().Bounds.Height) * 100f));

        }

        /// <summary>
        /// Draws 1st layer of user interface
        /// </summary>
        /// <param name="spb">spritebatch used to draw</param>
        /// <param name="engine">Engine object</param>
        /// <param name="w">World for this editor object</param>
        public void draw_static_containers(SpriteBatch spb, Engine engine, World w)
        {
            // draw GUI 
            GUI.draw_static_containers(spb, current_theme.interface_color, current_theme.interface_transparency);
        }
        /// <summary>
        /// Draw additional context elements in a separate process in order to place this in a different render surface independent of shader masking.
        /// This allows main context menu and subcontexts (expansions) to be placed anywhere on the screen, overlapping any element and not have part of itself erased by the shader effect.
        /// </summary>
        /// <param name="spb">spritebatch used to draw</param>
        /// <param name="engine">Engine object</param>
        /// <param name="w">World for this editor object</param>
        public void draw_context_containers_and_tooltips(SpriteBatch spb, Engine engine, World w)
        {
            // Add additional visual elements, e.g. text and selection matrix etc.
            Draw(spb, engine, w); // selection matrix exists here. Draw before context menu to keep it under UI
            // draw the rest of GUI 
            GUI.draw_context_containers_and_tooltips(spb, current_theme.interface_color, current_theme.interface_transparency);
        }
        /// <summary>
        /// Draw() function. Renders editor and its GUI
        /// </summary>
        /// <param name="spb">spritebatch used to draw</param>
        /// <param name="engine">Engine object</param>
        /// <param name="w">World for this editor object</param>
        public void Draw(SpriteBatch spb, Engine engine, World w)
        {
            if (selection_matrix_size() > 0 && selection_start_cell != null && selection_end_cell != null && !gui_move_mode)
            {// in order to remove GPU stress - draw only what's visible on screen 
                Vector2 real_start_cell = Vector2.Zero;
                Vector2 real_end_cell = Vector2.Zero;

                Vector2[] array = get_selection_real_start_end_cells();
                real_start_cell = array[0]; real_end_cell = array[1];

                // Draw Selection rectangle - coordinates on-screen
                Rectangle selection_crop = new Rectangle(
                    (int)engine.get_current_world().get_tile_origin(real_start_cell).X - (int)engine.get_camera_offset().X,
                    (int)engine.get_current_world().get_tile_origin(real_start_cell).Y - (int)engine.get_camera_offset().Y,
                    ((int)engine.get_current_world().get_tile_origin(real_end_cell).X - (int)engine.get_current_world().get_tile_origin(real_start_cell).X) + engine.get_current_world().tilesize,
                    ((int)engine.get_current_world().get_tile_origin(real_end_cell).Y - (int)engine.get_current_world().get_tile_origin(real_start_cell).Y) + engine.get_current_world().tilesize
                    );
                // if at least part of selection rectangle is visible (ignore part of the rectangle outside?)
                if (engine.get_viewport().Bounds.Intersects(selection_crop))
                {
                    // draw
                    engine.xna_draw(Engine.pixel, engine.get_current_world().get_tile_origin(real_start_cell) - engine.get_camera_offset(),
                    new Rectangle(0, 0, ((int)real_end_cell.X - (int)real_start_cell.X + 1) * engine.get_current_world().tilesize, ((int)real_end_cell.Y - (int)real_start_cell.Y + 1) * engine.get_current_world().tilesize),
                    selection_color * sel_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                    // draw borders
                    int thickness = 2;
                    float color_factor = 0.75f;
                    Vector2 st = engine.get_current_world().get_tile_origin(real_start_cell);
                    Vector2 en = engine.get_current_world().get_tile_origin(real_end_cell);
                    // top
                    engine.xna_draw(Engine.pixel, st - engine.get_camera_offset(),
                        new Rectangle(
                            0,
                            0,
                            (int)en.X - (int)st.X + engine.get_current_world().tilesize,
                            thickness),
                            engine.adjusted_color(selection_color, color_factor), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                    // bottom
                    engine.xna_draw(Engine.pixel, new Vector2(st.X, en.Y + engine.get_current_world().tilesize - thickness) - engine.get_camera_offset(),
                        new Rectangle(
                            0,
                            0,
                            (int)en.X - (int)st.X + engine.get_current_world().tilesize,
                            thickness),
                            engine.adjusted_color(selection_color, color_factor), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                    // right
                    engine.xna_draw(Engine.pixel, st - engine.get_camera_offset(),
                        new Rectangle(
                            0,
                            0,
                            thickness,
                            (int)en.Y - (int)st.Y + engine.get_current_world().tilesize
                            ),
                            engine.adjusted_color(selection_color, color_factor), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                    // left
                    engine.xna_draw(Engine.pixel, new Vector2(en.X + engine.get_current_world().tilesize - thickness, st.Y) - engine.get_camera_offset(),
                        new Rectangle(
                            0,
                            0,
                            thickness,
                            (int)en.Y - (int)st.Y + engine.get_current_world().tilesize
                            ),
                            engine.adjusted_color(selection_color, color_factor), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                }
            }
            // Preview added/deleted cells if GUI is not hovered
            if ((editor_mode == modes.add || editor_mode == modes.delete) && !GUI.hover_detect() && !editor_actions_locked && !gui_move_mode)
            {
                if (editor_tools == tools.radius)
                {
                    preview_tile((int)engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).X, (int)engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).Y, engine, submode_brush_radius, editor_mode);
                }
                else if (editor_tools == tools.line)
                {
                    preview_cell_matrix(engine, line_matrix);
                }
            }
            // draw current hovered cell coordinates at mouse
            if (!gui_move_mode && GUI.hover_detect() == false && engine.get_current_world().valid_cell(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)))
            {
                if (line_start_cell.X == -1) // no line tool 
                {
                    update_offset(0, -20);
                    string hovered_coordinates = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).ToString();
                    if (engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).X > -1)
                        engine.xna_draw_outlined_text(hovered_coordinates, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                // line tool
                else if (line_start_cell.X > -1)
                {
                    update_offset(0, -20);
                    string line_coordinates = line_start_cell.ToString();
                    string line_coordinates2 = line_end_cell.ToString();
                    string line_length = "line length: " + line_matrix.Count.ToString();
                    engine.xna_draw_outlined_text(line_coordinates, engine.get_current_world().get_tile_origin(line_start_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    engine.xna_draw_outlined_text(line_coordinates2, engine.get_current_world().get_tile_origin(line_end_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, 20); // draw line length below end cell
                    engine.xna_draw_outlined_text(line_length, engine.get_current_world().get_tile_origin(line_end_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }

                // draw submode name above mouse position
                if (editor_tools == tools.radius)
                {
                    string tool_text = "";
                    if (editor_mode == modes.add)
                    {
                        tool_text = "[add mode] brush tool";
                    }
                    else if (editor_mode == modes.delete)
                    {
                        tool_text = "[delete mode] brush tool";
                    }
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_tools == tools.line)
                {
                    string tool_text = "";
                    if (editor_mode == modes.add)
                    {
                        tool_text = "[add mode] line tool";
                    }
                    else if (editor_mode == modes.delete)
                    {
                        tool_text = "[delete mode] line tool ";
                    }
                    update_offset(0, -40);

                    if (line_start_cell.X != -1)
                        engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(line_end_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    else
                        engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_tools == tools.square)
                {
                    string tool_text = "";
                    if (editor_mode == modes.add)
                    {
                        tool_text = "[add mode] square tool";
                    }
                    else if (editor_mode == modes.delete)
                    {
                        tool_text = "[delete mode] square tool ";
                    }
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_tools == tools.hollow_square)
                {
                    string tool_text = "";
                    if (editor_mode == modes.add)
                    {
                        tool_text = "[add mode] hollow square tool";
                    }
                    else if (editor_mode == modes.delete)
                    {
                        tool_text = "[delete mode] hollow square tool ";
                    }
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_tools == tools.water)
                {
                    string tool_text = "{water tool}";
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, -60);
                    tool_text = "tile id: " + engine.get_current_world().get_tile_id(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine));
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, -80);
                    tool_text = "water content: " + engine.get_current_world().get_tile_water(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine));
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, -100);
                    tool_text = "pressure: " + engine.get_current_world().get_tile_pressure(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine));
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, -120);
                    tool_text = "source ( " + engine.get_current_world().get_tile_psource(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + " )";
                    engine.xna_draw_outlined_text(tool_text, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());

                }

                if (editor_mode == modes.select)
                {
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[selection mode] " + selection_matrix_size().ToString() + " cells", engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_mode == modes.prop_lights)
                {
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[lights mode]", engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                // draw line selection_start_cell coordinate at line start and draw line length
                // draw selection start cell coordinate, selection height, selection length and selection area
            }
        }

        public void draw_masking_layer()
        {
            GUI.draw_masking_layer();
        }

        public void draw_post_processing(Engine e, SpriteBatch sb)
        {
            GUI.draw_post_processing(e, current_theme.interface_color, current_theme.interface_transparency);
            GUI.get_text_engine().textengine_draw(e, ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_rectangle()); // draw text stored inside text engine
        }

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
            if (engine.get_current_world().not_tile_exists(x, y) || cell_overwrite_mode() || current == modes.delete)
            {
                if (engine.get_current_world().valid_cell(x, y))
                {
                    Rectangle current_cell_dimensions = engine.get_current_world().get_cell_rectangle_on_screen(engine, new Vector2(x, y));
                    Color mode_color = new Color();
                    if (current == modes.add)
                    {
                        mode_color = Color.LightGreen;
                    }
                    else if (current == modes.delete)
                    {
                        mode_color = Color.OrangeRed;
                    }

                    engine.xna_draw(Engine.pixel,
                    engine.get_current_world().get_tile_origin(new Vector2(x, y)) - engine.get_camera_offset(),
                    Engine.standard20, // rectangle crop
                    mode_color * engine.fade_sine_wave_smooth(3000, 0.65f, 0.75f, sinewave.one), 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }
            }

            return true;
        }

        public void preview_cell_matrix(Engine engine, List<Vector2> matrix)
        {
            Color highlight = Color.White;
            if (editor_mode == modes.add)
            {
                highlight = Color.LightGreen;
            }
            else if (editor_mode == modes.delete)
            {
                highlight = Color.OrangeRed;
            }

            for (int i = 0; i < matrix.Count; i++)
            {
                engine.xna_draw(Engine.pixel,
                        engine.get_current_world().get_tile_origin(matrix.ElementAt(i)) - engine.get_camera_offset(),
                        Engine.standard20, // rectangle crop
                        highlight * 0.5f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }

        public void update_offset(float x, float y)
        {
            engine_offset.X = x; engine_offset.Y = y;
        }

        // current edit tile id
        public void Update(Engine engine)
        {
            //UPDATE GUI ELEMENTS
            //GUI.set_interface_transparency(interface_transparency);
            GUI.Update(engine);
            GUI.find_element("INFOLABEL_BRUSH_RADIUS").set_label("brush size  " + ((Slider)GUI.find_element("SLIDER_BRUSHSIZE")).get_slider_value_int().ToString());
            GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON").set_icon(Tile.get_tile_struct(edit_tile_id).get_tile_icon_clip());
            GUI.find_element("INFOLABEL_CURRENT_EDIT_TILE").set_label(Tile.get_tile_struct(edit_tile_id).get_name());
            ((ColorPreviewButton)GUI.find_element("PREVIEW_SELECTIONCOLOR")).update(selection_color);
            ((ColorPreviewButton)GUI.find_element("PREVIEW_EDITORGRID")).update(engine.get_grid_color());
            // update slider values connected to this editor 
            // selection values
            sel_transparency = GUI.get_slider_value(actions.update_selection_transparency);
            selection_color.R = (byte)GUI.get_slider_value(actions.update_selection_color_red);
            selection_color.G = (byte)GUI.get_slider_value(actions.update_selection_color_green);
            selection_color.B = (byte)GUI.get_slider_value(actions.update_selection_color_blue);
            // brush values
            submode_brush_radius = (int)GUI.get_slider_value(actions.update_brush_size);
            // update choice values
            overwrite_cells = GUI.get_tracked_value<bool>("SWITCH_OVERWRITE_EXISTING_CELLS");
            engine.get_clock().set_paused(GUI.get_tracked_value<bool>("SWITCH_CLOCK_PAUSE_RESTART"));
            engine.set_lighting_state(GUI.get_tracked_value<bool>("SWITCH_ENABLE_WORLD_LIGHTING"));
            gui_move_mode = ((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).get_tracked_value(); // updates internal editor UI flag
            // update world percentage filled progress bar
            if (engine.get_frame_count() % 30 == 0) // limit update timing of this function
                ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).update((int)(engine.get_current_world().get_percent_filled() * 100f));

            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_X")).update((int)((engine.get_mouse_vector().X / (float)engine.get_viewport().Bounds.Width) * 100f)); // get mouse position X relative to Screen Width
            ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_Y")).update((int)((engine.get_mouse_vector().Y / (float)engine.get_viewport().Bounds.Height) * 100f)); // get mouse position Y relative to Screen Height

            // set boundaries for text area
            GUI.get_text_engine().set_target(((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_rectangle(), ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_origin());

            if (editor_mode == modes.select
                ||
                (
                    (editor_mode == modes.add || editor_mode == modes.delete)
                    &&
                    (editor_tools == tools.square || editor_tools == tools.hollow_square)
                ))
            {
                // selection matrix is used when:
                // 1. selection mode is active
                // 2. add mode is active and square or hollow square tool is active
                // 2A. if square tool is active - when user left-clicks once - fill all cells currently in selection, if hollow square - fill only the outer border.
                // 3. Right click cancels current selection
                selection_driver(engine);

                if (editor_mode == modes.select)
                    GUI.activate(modes.select);
            }
            else if (editor_mode == modes.add)
            {
                line_tool_driver(engine);
                GUI.activate(modes.add);
            }
            else if (editor_mode == modes.delete)
            {
                line_tool_driver(engine);
                GUI.activate(modes.delete);
            }
            else if (editor_mode == modes.prop_lights)
            {
                GUI.activate(modes.prop_lights);
            }
            // editor tool dependencies
            if (editor_tools != tools.radius) // hide brush size option from view
            {
                GUI.find_container("CONTAINER_BRUSH_SIZE_OPTIONS").set_visibility(false);
            }
            else
            {
                GUI.find_container("CONTAINER_BRUSH_SIZE_OPTIONS").set_visibility(true);
            }

            // outline containers based on current move mode
            if (gui_move_mode)
            {
                GUI.set_container_outline(true);
            }
            else
            {
                GUI.set_container_outline(false);
            }

            // update selection color surrogate
            selection_color_surrogate = TextEngine.color_to_delimited_string(selection_color);

        }
        // World Editor functions
        // Send a mouse keyboard input to editor class and execute an actions specified by the User Interface element clicked/used
        public void editor_command(Engine engine, command c)
        {
            if (!locked) // get new action unless last action has been locked in
                action = GUI.ui_command(c); // get current_action associated with a button/element

            //Tools are locked when overall context menu is opened. When it'engine closed tools are unlocked on mouse release so that nothing is changed in the world on accident.
            if (editor_actions_locked && !contexts_visible())
            {
                if (c == command.left_release)
                {
                    editor_actions_locked = false;
                }
            }
            else if (editor_actions_locked && contexts_visible())
            {
                if (c == command.left_release && GUI.hover_detect() == false)
                {// subcontexts are visible and click happened outside any container or element = hide contexts and unlock GUI
                    hide_all_contexts();
                    editor_actions_locked = false;
                }
            }
            // calculate current hovered cell
            Vector2 active_cell = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine);

            // hover independent section or command driven events
            // hotkeys and shortcuts - enable/uncomment when shortcuts are created
            switch (c)
            {
                case command.left_release:
                    {   // releases UI lock when mouse is released if it hasn't been released for whatever reason
                        set_hostUI_lock(false);
                    }
                    break;
                case command.left_hold:
                    {   // if move mode and container is hovered - only move container and return function without doing anything else
                        if (gui_move_mode && GUI.hover_detect() && GUI.get_hovered_container() != null)
                        {
                            set_hostUI_lock(true);
                            try
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {   // move any elements except the locker itself
                                    if (GUI.get_hovered_element_id() != "LOCKER_UI_MOVEMENT")
                                    {
                                        Vector2 container_position = GUI.get_hovered_container().get_origin(); // get current container position
                                        GUI.get_hovered_container().set_origin(container_position + engine.get_mouse_displacement()); // updates positioning
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                            catch (NullReferenceException)
                            {
                                // do nothing - this should never happen - include in code for safety reasons
                            }
                        }
                    }
                    break;
                case command.alt_q: // cycle submode left
                    submode_decrease();
                    hide_expandable_containers_only();
                    break;
                case command.alt_e: // cycle submode right
                    submode_increase();
                    hide_expandable_containers_only();
                    break;
                case command.alt_1:
                    clear_selection();
                    hide_expandable_containers_only();
                    clear_line_mode();
                    switch_mode("add");
                    break;
                case command.alt_2:
                    clear_selection();
                    hide_expandable_containers_only();
                    clear_line_mode();
                    switch_mode("delete");
                    break;
                case command.alt_3:
                    clear_selection();
                    hide_expandable_containers_only();
                    clear_line_mode();
                    switch_mode("select");
                    break;
                case command.alt_4:
                    clear_selection();
                    hide_expandable_containers_only();
                    clear_line_mode();
                    switch_mode("lights");
                    break;
                case command.mouse_scroll_up:
                    {
                        if (GUI.hover_detect())
                            GUI.get_hovered_container().scroll_up();
                    }
                    break;
                case command.mouse_scroll_down:
                    {
                        if (GUI.hover_detect())
                            GUI.get_hovered_container().scroll_down();
                    }
                    break;
                case command.enter_key:
                    {
                        if (current_focused != null && current_focused.get_text().Length > 0) // make sure there is a text input active
                        {// then send text info to text area and erase current data in the text input
                            //((TextArea)GUI.find_element(current_focused.get_input_target_id())).accept_text_output(current_focused.get_text()); // send input
                            GUI.get_text_engine().add_message_element(engine, "system[0,75,220] ( ~time ): " + current_focused.get_text()); // adding a coded system word to input ot signify a source
                            current_focused.clear_text();
                        }
                    }
                    break;
                default:
                    break;
            }

            // hover/no hover dependent section  
            //==============================================ACTION SWITCH
            //===========================================================
            // move mode dependent (if move mode is enabled only these actions are allowed)
            if (gui_move_mode)
            {
                // move mode lock/unlock button
                switch (action)
                {
                    case actions.unlock_ui_move:
                        {
                            if (c == command.left_release)
                                ((UIlocker)GUI.get_hovered_element()).toggle_lock();
                        }
                        break;
                    default:
                        break;
                }
            }
            // move mode independent (all actions allowed when move mode is inactive)
            else
            {
                switch (action)
                {
                    //--------------------------------------------------------- NO HOVER - MODE DEPENDENT
                    case null: // null means no element is hovered - do something with the world
                        {
                            switch (editor_mode)
                            {
                                // DEPENDENT ON ADD MODE
                                case modes.add:
                                    {
                                        if (c == command.left_click || c == command.left_hold)
                                        {
                                            if (editor_tools == tools.radius && !editor_actions_locked)
                                            {
                                                clear_selection();
                                                engine.get_current_world().generate_tile(edit_tile_id, (int)active_cell.X, (int)active_cell.Y, engine, submode_brush_radius, 0);
                                            }
                                            else if (editor_tools == tools.line && !editor_actions_locked && c == command.left_click)
                                            {
                                                clear_selection();
                                                if (line_start_cell.X == -1) // no start cell defined - create one
                                                {
                                                    line_start_cell.X = active_cell.X;
                                                    line_start_cell.Y = active_cell.Y;
                                                }
                                                else //generate ui_elements, then remove end cell and reassign start cell to previous end cell. Moving mouse will automatically assign end cell each time
                                                {
                                                    engine.get_current_world().generate_matrix(line_matrix, engine, edit_tile_id);
                                                    line_start_cell.X = line_end_cell.X;
                                                    line_start_cell.Y = line_end_cell.Y;
                                                    line_end_cell.X = -1;
                                                    line_end_cell.Y = -1;
                                                }
                                            }
                                            else if ((editor_tools == tools.square || editor_tools == tools.hollow_square) && !editor_actions_locked)
                                            {
                                                if (c == command.left_click)
                                                {
                                                    if (selection_start_cell == null) // assign start point
                                                    {
                                                        selection_start_cell = active_cell;
                                                        selection_end_cell = null;
                                                    }
                                                }
                                                else if (c == command.right_click || c == command.right_hold)
                                                {
                                                    clear_selection();
                                                }
                                                else
                                                {
                                                    selection_end_cell = active_cell;
                                                }
                                            }
                                            else if (editor_tools == tools.water) // NOTE: testing WATER generation
                                            {
                                                clear_selection();
                                                engine.get_current_world().generate_tile(-1, (int)active_cell.X, (int)active_cell.Y, engine, submode_brush_radius, engine.generate_int_range(50, 100));
                                            }
                                        }
                                        else if (c == command.right_click || c == command.right_hold)
                                        {
                                            if (editor_tools == tools.line && !editor_actions_locked)
                                            {
                                                line_start_cell.X = -1; // -1 will be treated as null by the driver function
                                                line_start_cell.Y = -1;
                                                line_matrix.Clear();
                                            }
                                        }
                                        else if (c == command.left_release)
                                        {
                                            if (selection_start_cell != null && selection_end_cell != null) // assign end point and create ui_elements, then clear selection
                                            {
                                                selection_end_cell = active_cell;

                                                if (editor_tools == tools.square)
                                                    engine.get_current_world().generate_matrix(selection_matrix, engine, edit_tile_id);
                                                else if (editor_tools == tools.hollow_square)
                                                    engine.get_current_world().generate_hollow_matrix(selection_matrix, engine, edit_tile_id);

                                                clear_selection();
                                            }
                                        }
                                    }
                                    break;
                                // DEPENDENT ON DELETE MODE
                                case modes.delete:
                                    {
                                        if (c == command.left_click || c == command.left_hold)
                                        {
                                            if (editor_tools == tools.radius && !editor_actions_locked)
                                            {
                                                engine.get_current_world().erase_tile((int)active_cell.X, (int)active_cell.Y, engine, submode_brush_radius);
                                            }
                                            else if (editor_tools == tools.line && !editor_actions_locked && c == command.left_click)
                                            {
                                                if (line_start_cell.X == -1) // no start cell defined - create one
                                                {
                                                    line_start_cell.X = active_cell.X;
                                                    line_start_cell.Y = active_cell.Y;
                                                }
                                                else //generate ui_elements, then remove end cell and reassign start cell to previous end cell. Moving mouse will automatically assign end cell each time
                                                {
                                                    engine.get_current_world().erase_matrix(line_matrix, engine);
                                                    line_start_cell.X = line_end_cell.X;
                                                    line_start_cell.Y = line_end_cell.Y;
                                                    line_end_cell.X = -1;
                                                    line_end_cell.Y = -1;
                                                }
                                            }
                                            else if ((editor_tools == tools.square || editor_tools == tools.hollow_square) && !editor_actions_locked)
                                            {
                                                if (c == command.left_click)
                                                {
                                                    if (selection_start_cell == null) // assign start point
                                                    {
                                                        selection_start_cell = active_cell;
                                                        selection_end_cell = null;
                                                    }
                                                }
                                                else if (c == command.right_click || c == command.right_hold)
                                                {
                                                    clear_selection();
                                                }
                                                else
                                                {
                                                    selection_end_cell = active_cell;
                                                }
                                            }
                                        }
                                        else if (c == command.right_click || c == command.right_hold)
                                        {
                                            if (editor_tools == tools.line && !editor_actions_locked)
                                            {
                                                line_start_cell.X = -1; // -1 will be treated as null by the driver function
                                                line_start_cell.Y = -1;
                                                line_matrix.Clear();
                                            }
                                        }
                                        else if (c == command.left_release)
                                        {
                                            if (selection_start_cell != null && selection_end_cell != null) // assign end point and create ui_elements, then clear selection
                                            {
                                                selection_end_cell = active_cell;

                                                if (editor_tools == tools.square)
                                                    engine.get_current_world().erase_matrix(selection_matrix, engine);
                                                else if (editor_tools == tools.hollow_square)
                                                    engine.get_current_world().erase_hollow_matrix(selection_matrix, engine);

                                                clear_selection();
                                            }
                                        }
                                    }
                                    break;
                                // DEPENDENT ON SELECT MODE
                                case modes.select:
                                    {
                                        if (!editor_actions_locked)
                                        {
                                            if (c == command.left_click)
                                            {
                                                selection_start_cell = active_cell;
                                                selection_end_cell = null;
                                            }
                                            else if (c == command.left_release || c == command.left_hold)
                                                selection_end_cell = active_cell;
                                        }
                                    }
                                    break;
                                case modes.prop_lights:
                                    {
                                        clear_selection();
                                        if (c == command.left_click && !editor_actions_locked)
                                        {
                                            engine.get_current_world().generate_light_source(new Color(engine.generate_int_range(0, 0), engine.generate_int_range(0, 255), engine.generate_int_range(0, 255)), engine.get_current_mouse_state(), engine, 600, 0.65f);
                                        }
                                    }
                                    break;
                                default:
                                    {
                                        hide_expandable_containers_only();
                                        clear_selection();
                                    }
                                    break;
                            }
                            //--------------------------------------------------------- NO HOVER - MODE INDEPENDENT
                            // actions independent of editor mode and hover independent
                            switch (c)
                            {
                                case command.left_click:
                                    {
                                        //current_focused.clear_text(); // remove input text from element 
                                        if (current_focused != null)
                                            current_focused.set_focus(false);

                                        current_focused = null; // if there is no hover - current focus should be unfocused
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }// end case null:
                        break;
                    //--------------------------------------------------------- HOVER - MODE INDEPENDENT
                    // below are GUI actions only executed if part of GUI was hovered during a click
                    case actions.clear_all:
                        {
                            if (c == command.left_click) // confirmation functionality example
                            {
                                if (GUI.get_hovered_element().required_confirmation())
                                {
                                    confirm_action(engine, new Vector2(GUI.find_unit(actions.clear_all).get_origin().X + GUI.find_unit(actions.clear_all).get_rectangle().Width, GUI.find_unit(actions.clear_all).get_origin().Y), actions.clear_all);
                                }
                                else
                                {
                                    delete_all_cells(engine);
                                    hide_all_contexts(); // not needed after action is taken
                                }
                            }
                        }
                        break;
                    case actions.fill_all:
                        {
                            if (c == command.left_click)
                            {
                                if (GUI.get_hovered_element().required_confirmation()) // confirmation selection structure
                                {
                                    confirm_action(engine, new Vector2(GUI.find_unit(actions.fill_all).get_origin().X + GUI.find_unit(actions.fill_all).get_rectangle().Width, GUI.find_unit(actions.fill_all).get_origin().Y), actions.fill_all);
                                }
                                else
                                {
                                    fill_all_cells(engine);
                                    hide_all_contexts(); // not needed after action is taken
                                }
                            }
                        }
                        break;
                    case actions.editor_mode_switch_add:
                        {
                            clear_selection();
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_line_mode();

                            if (editor_mode != modes.add)
                            {
                                if (c == command.left_click)
                                {
                                    switch_mode("add");
                                    editor_tools = tools.radius;
                                }
                            }
                            else // if mode is already add - cycle submodes every click
                            {
                                if (c == command.left_click)
                                    submode_increase();
                            }
                        }
                        break;
                    case actions.editor_mode_switch_delete:
                        {
                            clear_selection();
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_line_mode();

                            if (editor_mode != modes.delete)
                            {
                                if (c == command.left_click)
                                {
                                    switch_mode("delete");
                                    editor_tools = tools.radius;
                                }
                            }
                        }
                        break;
                    case actions.editor_mode_switch_select:
                        {
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_selection();
                            clear_line_mode();

                            switch_mode("select");
                        }
                        break;
                    case actions.editor_mode_switch_lights:
                        {
                            clear_selection();
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_line_mode();

                            switch_mode("lights");
                        }
                        break;
                    // new actions - various resolutions and modes
                    case actions.resolution_fullscreen:
                        engine.getGame1().make_fullscreen(true);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_fullscreen_reverse:
                        engine.getGame1().make_fullscreen(false);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_1920_1080:
                        engine.getGame1().update_resolution(1920,1080);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_1440_900:
                        engine.getGame1().update_resolution(1440, 900);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_1366_768:
                        engine.getGame1().update_resolution(1366, 768);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_1280_800:
                        engine.getGame1().update_resolution(1280, 800);
                        hide_expandable_containers_only();
                        break;
                    case actions.resolution_1024_576:
                        engine.getGame1().update_resolution(1024, 576);
                        hide_expandable_containers_only();
                        break;
                    // END // various resolutions and modes
                    case actions.go_to_world_origin:
                        engine.set_camera_offset(new Vector2(-engine.get_viewport().Width/2, -engine.get_viewport().Height/2)); // reset camera offset
                        break;
                    case actions.unlock_ui_move:
                        {
                            if (c == command.left_release)
                                ((UIlocker)GUI.get_hovered_element()).toggle_lock();
                        }
                        break;
                    case actions.toggle_subcontext:
                        {
                            editor_actions_locked = true; // locks editor map changing tools
                            // hide a confirmation dialog, because a different option has been clicked
                            GUI.find_container("confirmation box", true).set_visibility(false);
                            // summon a slider window that will change grid transparency
                            Rectangle context = GUI.get_hovered_container().get_rectangle();
                            Vector2 option = GUI.get_hovered_element().get_origin();

                            //GUI.change_container_origin("sub", new Vector2(context.X + context.Width + 1, option.Y)); // find coordinates of context menu and position of an option button
                            if (GUI.get_hovered_element() is Button)
                            {
                                GUI.update_sub_context_origin(((Button)GUI.get_hovered_element()).get_context(), engine.get_viewport(), new Vector2(context.X + context.Width, option.Y));

                                if (c == command.left_click)
                                {
                                    hide_expandable_containers_only(); // hide all other submenus

                                    ((Button)GUI.get_hovered_element()).get_context().make_visible(engine); // summons a context menu assigned to current hovered unit
                                }
                            }
                        }
                        break;
                    case actions.update_slider_grid_transparency:
                        {
                            set_hostUI_lock(true); // locking functionality example
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_grid_transparency)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_grid_transparency), engine.get_mouse_vector());
                                }
                                else
                                {
                                    set_hostUI_lock(false); // unlock UI
                                }
                            }
                        }
                        break;
                    case actions.update_slider_grid_color_red:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_grid_color_red)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_grid_color_red), engine.get_mouse_vector());
                                }
                                else
                                {
                                    set_hostUI_lock(false);
                                }
                            }
                        }
                        break;
                    case actions.update_slider_grid_color_green:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_grid_color_green)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_grid_color_green), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_slider_grid_color_blue:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_grid_color_blue)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_grid_color_blue), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_selection_transparency:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_selection_transparency)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_selection_transparency), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_selection_color_red:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_selection_color_red)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_selection_color_red), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_selection_color_green:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_selection_color_green)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_selection_color_green), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_selection_color_blue:
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_selection_color_blue)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_selection_color_blue), engine.get_mouse_vector());
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.switch_theme:
                        {// assign current theme
                            try
                            {
                                current_theme = find_theme(((IDButton<string>)GUI.get_hovered_element()).get_tracked_value());
                            }
                            catch (NullReferenceException)
                            {
                                // nothing returned
                            }
                            catch (InvalidCastException)
                            {
                                // wrong type of ui element
                            }
                        }
                        break;
                    case actions.update_brush_size:
                        {
                            set_hostUI_lock(true);
                            // change the slider value
                            if (c == command.left_hold)
                            {
                                GUI.update_slider_values(GUI.find_unit(actions.update_brush_size), engine.get_mouse_vector());
                            }
                            else
                                set_hostUI_lock(false);
                        }
                        break;
                    case actions.current_container_random_sliders:
                        {
                            // randomize all slider values for current container (do not hide context because this is a repeatable function)
                            if (c == command.left_click)
                                randomize_sliders(engine);
                        }
                        break;
                    case actions.overall_context:
                        {
                            if (line_start_cell.X == -1)
                            {
                                hide_all_contexts(); // close all other menus before opening this one again
                                GUI.change_container_origin(engine.get_viewport(), "context menu", engine.get_mouse_vector() + Vector2.One); // create almost at mouse, to avoid inital hover

                                if (c == command.right_click) // ignore if something is in line tool start cell
                                    GUI.set_container_visibility("context menu", true);

                                // lock editor tools
                                editor_actions_locked = true;
                            }
                        }
                        break;
                    case actions.option_value_switch:
                        {
                            // for current tracked value of bool contexttype - switch this value
                            if (c == command.left_click)
                            {
                                if (GUI.get_hovered_element() is SwitchButton<bool>)
                                {
                                    SwitchButton<bool> temp = (SwitchButton<bool>)GUI.get_hovered_element();
                                    temp.set_value(!temp.get_tracked_value());
                                }
                            }
                        }
                        break;
                    case actions.change_cell_design:
                        {
                            if (GUI.get_hovered_element() is IDButton<short> && c == command.left_click)
                            {
                                edit_tile_id = ((IDButton<short>)GUI.get_hovered_element()).get_tracked_value();
                                hide_all_contexts();
                            }
                        }
                        break;
                    case actions.hide_menu:
                        hide_all_contexts();
                        break;
                    case actions.hide_this_container:
                        GUI.get_hovered_container().set_visibility(false); // hides any container that has a "close" or "cancel" button with this action assigned to it
                        break;
                    case actions.focus_input:
                        // SIMPLE ACTION = assign current hovered element to focused input of this editor. Exception block is needed in case UI element is mistakenly marked with this action
                        try
                        {
                            current_focused = (TextInput)GUI.get_hovered_element();
                            current_focused.set_focus(true); // mark element focused internally 
                        }
                        catch (InvalidCastException)
                        {
                            current_focused = null;
                        }
                        break;
                    default:
                        break;
                }//end of action switch statement
            }//end of move mode independent section
        }//end of function


        // support functions 
        /// <summary>
        /// Editor will accept a keyboard input as characters if there is an active text input target 
        /// </summary>
        /// <param name="value"></param>
        public void accept_input(Engine engine, string value)
        {
            if (current_focused == null)
                return;

            long current = engine.get_current_game_millisecond();
            string last = current_focused.get_last_input();

            if (!String.Equals(last, value)) // different input
            {
                if (current_focused != null)
                {
                    current_focused.add_text(value);
                    last_input_timestamp = current; // assign new value to most recent input
                }
            }
            else
            {
                if (last_input_timestamp + input_delay <= current)
                {
                    if (current_focused != null)
                    {
                        current_focused.add_text(value);
                        last_input_timestamp = engine.get_current_game_millisecond(); // assign new value to most recent input
                    }
                }
            }
        }

        public bool accepting_input()
        {
            return (current_focused != null);
        }
        /// <summary>
        /// Sets last input received timestamp to 0
        /// </summary>
        public void reset_focused_input_delay()
        {
            last_input_timestamp = 0;
        }

        public void refresh_focused_input_delay(Engine engine)
        {
            last_input_timestamp = engine.get_current_game_millisecond(); // to keep delay active in case there was no input
        }

        public void erase_one_character_from_input(Engine engine)
        {
            if ((last_backspace_timestamp + backspace_delay <= engine.get_current_game_millisecond()))
            {
                current_focused.erase_one_character();
                last_backspace_timestamp = engine.get_current_game_millisecond();
            }
        }

        public TextInput get_current_input_target()
        {
            return current_focused;
        }
        public color_theme find_theme(string id)
        {
            foreach (color_theme t in themes)
            {
                if (t.id == id)
                {
                    return t;
                }
            }
            return default(color_theme);
        }

        public color_theme get_current_theme()
        {
            return current_theme;
        }

        public float get_selection_transparency()
        {
            return sel_transparency;
        }
        public Color get_selection_color()
        {
            return selection_color;
        }
        // functions executed by "editor_command"
        public void delete_all_cells(Engine engine)
        {
            for (int i = 1; i <= engine.get_current_world().length; i++)
            {
                for (int j = 1; j <= engine.get_current_world().height; j++)
                {
                    engine.get_current_world().erase_tile(i, j, engine);
                }
            }
        }
        public void fill_all_cells(Engine engine)
        {
            for (int i = 1; i <= engine.get_current_world().length; i++)
            {
                for (int j = 1; j <= engine.get_current_world().height; j++)
                {
                    if (!engine.get_current_world().tile_exists(new Vector2(i, j)))
                        engine.get_current_world().generate_tile(edit_tile_id, i, j, engine);
                }
            }
        }

        public void submode_decrease()
        {
            line_start_cell.X = line_start_cell.Y = -1;
            line_matrix.Clear();
            selection_matrix.Clear();
            if (editor_tools == 0)
            {
                editor_tools = (tools)Enum.GetValues(typeof(tools)).Length - 1;
            }
            else
                editor_tools--;
        }
        public void submode_increase()
        {
            line_start_cell.X = line_start_cell.Y = -1;
            line_matrix.Clear();
            selection_matrix.Clear();
            if (editor_tools == (tools)Enum.GetValues(typeof(tools)).Length - 1)
            {
                editor_tools = 0;
            }
            else
                editor_tools++;
        }

        public bool cell_overwrite_mode()
        {
            return overwrite_cells;
        }
        public void set_overwrite_mode(bool value)
        {
            overwrite_cells = value;
        }

        public short get_current_editor_cell()
        {
            return edit_tile_id;
        }

        public Vector2[] get_selection_real_start_end_cells()
        {
            Vector2 real_start_cell = Vector2.Zero;
            Vector2 real_end_cell = Vector2.Zero;

            if (((Vector2)selection_start_cell).X <= ((Vector2)selection_end_cell).X && ((Vector2)selection_end_cell).Y > ((Vector2)selection_start_cell).Y) // 1
            {
                real_start_cell.X = ((Vector2)selection_start_cell).X;
                real_start_cell.Y = ((Vector2)selection_start_cell).Y;

                real_end_cell.X = ((Vector2)selection_end_cell).X;
                real_end_cell.Y = ((Vector2)selection_end_cell).Y;
            }
            else if (((Vector2)selection_start_cell).X <= ((Vector2)selection_end_cell).X && ((Vector2)selection_start_cell).Y >= ((Vector2)selection_end_cell).Y) // 3
            {
                real_start_cell.X = ((Vector2)selection_start_cell).X;
                real_start_cell.Y = ((Vector2)selection_end_cell).Y;

                real_end_cell.X = ((Vector2)selection_end_cell).X;
                real_end_cell.Y = ((Vector2)selection_start_cell).Y;
            }
            else if (((Vector2)selection_start_cell).X > ((Vector2)selection_end_cell).X && ((Vector2)selection_end_cell).Y > ((Vector2)selection_start_cell).Y) //2
            {
                real_start_cell.X = ((Vector2)selection_end_cell).X;
                real_start_cell.Y = ((Vector2)selection_start_cell).Y;

                real_end_cell.X = ((Vector2)selection_start_cell).X;
                real_end_cell.Y = ((Vector2)selection_end_cell).Y;
            }
            else if (((Vector2)selection_start_cell).X > ((Vector2)selection_end_cell).X && ((Vector2)selection_start_cell).Y >= ((Vector2)selection_end_cell).Y) //4
            {
                real_start_cell.X = ((Vector2)selection_end_cell).X;
                real_start_cell.Y = ((Vector2)selection_end_cell).Y;

                real_end_cell.X = ((Vector2)selection_start_cell).X;
                real_end_cell.Y = ((Vector2)selection_start_cell).Y;
            }

            Vector2[] results = new Vector2[2];
            results[0] = real_start_cell;
            results[1] = real_end_cell;
            return results;
        }

        /// <summary>
        /// Adds a confirmation dialog box functionality to GUI
        /// </summary>
        /// <param name="engine">Engine</param>
        /// <param name="proposed_origin">position at which this container shoudl appear</param>
        /// <param name="action">action assigned to "ok" button</param>
        public void confirm_action(Engine engine, Vector2 proposed_origin, actions action)
        {
            // hides confirmation box if it already exists for another option
            GUI.find_container("confirmation box", true).set_visibility(false);
            // hides subcontexts if they exist
            hide_expandable_containers_only();
            // change confirmation dialogue label and assigned action
            GUI.find_unit("ok").set_action(action); // other button will have a hide container functionality
            // summon confirmation dialogue (clicking on yes in that dialog will execute same action as previous button , but it will not require a confirmation anymore)
            GUI.update_sub_context_origin(GUI.find_container("confirmation box", true), engine.get_viewport(), proposed_origin);
            GUI.find_container("confirmation box", true).set_visibility(true); // show this Container
        }
        /// <summary>
        /// This function is created in a class hosting a GUI.
        /// To lock in a slider function even when mouse leaves slider area bounds (but has started there). IMPORTANT lock to keep sliders user-friendly.
        /// set this lock to true. Release when comman is no longer e.g. command.left_hold
        /// </summary>
        /// <param name="value">bool value true/false. True enables lock, false - removes lock</param>
        public void set_hostUI_lock(bool value)
        {
            locked = value;
            GUI.set_lock_state(value);
        }
        /// <summary>
        /// Host class slider randomizing function for GUI
        /// </summary>
        /// <param name="engine">Engine Helper with "random function" inside</param>
        public void randomize_sliders(Engine engine)
        {
            foreach (UIElementBase u in GUI.get_hovered_container().get_element_list())
            {
                if (u.get_type() == type.slider)
                {
                    if (u is Slider)
                    {
                        Slider temp = (Slider)u;
                        temp.set_random_slider_value(engine);
                    }
                }
            }
        }
        public void switch_mode(String mode)
        {
            if (mode == "add")
            {
                editor_mode = modes.add;
            }
            else if (mode == "delete")
            {
                editor_mode = modes.delete;
            }
            else if (mode == "select")
            {
                editor_mode = modes.select;
            }
            else if (mode == "lights")
            {
                editor_mode = modes.prop_lights;
            }
        }
        // hide context
        public void hide_all_contexts()
        {
            foreach (Container c in GUI.get_containers())
            {
                if (c.is_visible() && (c.get_context_type() == context_type.context || c.get_context_type() == context_type.expansion))
                {
                    c.set_visibility(false);
                }
            }
        }
        public void hide_expandable_containers_only()
        {
            foreach (Container c in GUI.get_containers())
            {
                if (c.is_visible() && c.get_context_type() == context_type.expansion /*&& GUI.get_hovered_element() is Button*/)
                {
                    /*if(((Button)GUI.get_hovered_element()).get_context() != c)*/
                    c.set_visibility(false);
                }
            }
        }

        public void clear_line_mode()
        {
            line_start_cell.X = -1; // -1 will be treated as null by the driver function
            line_start_cell.Y = -1;
            line_matrix.Clear();
            editor_tools = tools.radius;
        }
        /// <summary>
        /// Determine if any contexts are visible on-screen
        /// </summary>
        /// <returns></returns>
        public bool contexts_visible()
        {
            foreach (Container c in GUI.get_containers())
            {
                if (c.is_visible() && (c.get_context_type() == context_type.context || c.get_context_type() == context_type.expansion))
                {
                    return true;
                }
            }

            return false;
        }

        public void line_tool_driver(Engine engine)
        {
            if (line_start_cell.X == -1)
                return;

            Vector2 cell = engine.get_world_list().get_current().get_current_hovered_cell(engine.get_current_mouse_state(), engine); // checks which cell is being hovered in the current world
            //line_end_cell = cell;
            line_matrix.Clear();

            int x_difference = Math.Abs((int)cell.X - (int)line_start_cell.X);
            int y_difference = Math.Abs((int)cell.Y - (int)line_start_cell.Y);

            line_matrix.Add(line_start_cell);

            if (x_difference >= y_difference) // horizontal line
            {
                if (cell.X <= line_start_cell.X) // left direction
                {
                    for (int i = (int)cell.X; i < (int)line_start_cell.X; i++)
                    {
                        line_matrix.Add(new Vector2(i, line_start_cell.Y));
                    }
                }
                else // right direction 
                {
                    for (int i = (int)line_start_cell.X + 1; i <= (int)cell.X; i++)
                    {
                        line_matrix.Add(new Vector2(i, line_start_cell.Y));
                    }
                }
                // assign last hovered cell as new start point
                line_end_cell.X = cell.X;
                line_end_cell.Y = line_start_cell.Y;
            }
            else // vertical line
            {
                if (cell.Y <= line_start_cell.Y) // top direction
                {
                    for (int i = (int)cell.Y; i < (int)line_start_cell.Y; i++)
                    {
                        line_matrix.Add(new Vector2(line_start_cell.X, i));
                    }
                }
                else // bottom direction 
                {
                    for (int i = (int)line_start_cell.Y + 1; i <= (int)cell.Y; i++)
                    {
                        line_matrix.Add(new Vector2(line_start_cell.X, i));
                    }
                }
                // assign last hovered cell as new start point
                line_end_cell.X = line_start_cell.X;
                line_end_cell.Y = cell.Y;
            }
        }
        public string get_selection_color_surrogate()
        {
            return selection_color_surrogate;
        }
        public Color get_interface_color()
        {
            return current_theme.interface_color;
        }

        public float get_interface_transparency()
        {
            return current_theme.interface_transparency;
        }
        // matrix size
        public int selection_matrix_size()
        {
            return selection_matrix.Count;
        }
        // remove selection
        public void clear_selection()
        {
            selection_start_cell = null;
            selection_end_cell = null;
            selection_matrix.Clear();
        }

        // remove focus from inputs
        // needed to prevent stuck aswd keys
        public void unfocus_inputs()
        {
            try
            {
                current_focused.clear_text(); // remove text
            }
            catch (NullReferenceException)
            {
                // do nothing
            }
            finally
            {
                current_focused = null;       // clear focus from input
            }
        }
        // selection matrix handling function
        public void selection_driver(Engine engine)
        {
            // calculate if selection is in a proper range
            if (selection_start_cell == null || selection_end_cell == null || !engine.get_current_world().valid_cell((Vector2)(selection_end_cell)) || !engine.get_current_world().valid_cell((Vector2)(selection_start_cell)))
                return;

            Vector2 beginning = (Vector2)selection_start_cell;
            Vector2 end = (Vector2)selection_end_cell;

            // clear matrix
            selection_matrix.Clear();
            // calculations - refresh selection matrix
            if (beginning.X < end.X) // start vector X first
            {
                if (end.Y < beginning.Y) // end vector Y first
                {
                    for (int i = (int)beginning.X; i <= (int)end.X; i++)
                    {
                        for (int j = (int)end.Y; j <= (int)beginning.Y; j++)
                        {
                            selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                        }
                    }
                }
                else if (beginning.Y <= end.Y) // start vector Y first
                {
                    for (int i = (int)beginning.X; i <= (int)end.X; i++)
                    {
                        for (int j = (int)beginning.Y; j <= (int)end.Y; j++)
                        {
                            selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                        }
                    }
                }
            }
            else if (end.X < beginning.X) // end vector X first
            {
                if (end.Y < beginning.Y) // end vector Y first
                {
                    for (int i = (int)end.X; i <= (int)beginning.X; i++)
                    {
                        for (int j = (int)end.Y; j <= (int)beginning.Y; j++)
                        {
                            selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                        }
                    }
                }
                else if (beginning.Y <= end.Y) // start vector Y first
                {
                    for (int i = (int)end.X; i <= (int)beginning.X; i++)
                    {
                        for (int j = (int)beginning.Y; j <= (int)end.Y; j++)
                        {
                            selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                        }
                    }
                }
            }
            else // equal 
            {
                if (end.Y < beginning.Y) // end vector Y first
                {
                    int i = (int)beginning.X;

                    for (int j = (int)end.Y; j <= (int)beginning.Y; j++)
                    {
                        selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                    }
                }
                else if (beginning.Y <= end.Y) // start vector Y first
                {
                    int i = (int)beginning.X;

                    for (int j = (int)beginning.Y; j <= (int)end.Y; j++)
                    {
                        selection_matrix.Add(new Vector2(i, j)); // add cell to selection matrix
                    }
                }
            }
        }

        public void seed_interface_with_serialized_data(List<Container> deserialized_list)
        {
            try
            {
                if (deserialized_list.Count == 0)
                    return;

                // find all serialized containers and assign their positions to current default containers
                foreach (Container c in deserialized_list)
                {
                    c.deserialize_rectangle_surrogate_string(); // recreates rectangle in deserialized copy of container
                    GUI.find_container(c.get_id()).set_origin(c.get_origin()); // assign exact origin of serialized containers from previous run
                }
            }
            catch(NullReferenceException e)
            {
                // no user interface info saved - use defaults
            }
        }

        public void seed_interface_with_color_data(Editor deserialized_editor)
        {
            if (deserialized_editor == null)
                return;

            current_theme = deserialized_editor.get_current_theme();
            current_theme.deserialize_color_string();
            sel_transparency = 0.5f;

            // attempt to resurrcet selection color data
            try
            {
                selection_color = TextEngine.delimited_string_to_color(deserialized_editor.get_selection_color_surrogate());
            }
            catch(NullReferenceException e)
            {
                selection_color = current_theme.get_color(); // if it doesn't exist in the serialized copy - assign default value
            }

            // update sliders
            ((Slider)GUI.find_unit(actions.update_selection_transparency)).set_slider_value(sel_transparency);
            ((Slider)GUI.find_unit(actions.update_selection_color_red)).set_slider_value(selection_color.R);
            ((Slider)GUI.find_unit(actions.update_selection_color_green)).set_slider_value(selection_color.G);
            ((Slider)GUI.find_unit(actions.update_selection_color_blue)).set_slider_value(selection_color.B);
        }

    }// class end
}// namespace end