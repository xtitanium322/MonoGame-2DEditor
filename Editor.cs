using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace EditorEngine
{
    /// <summary>
    /// Interface color theme templates
    /// </summary>
    [Serializable()]
    public struct color_theme
    {
        public string id; // identifier for this theme
        [NonSerialized] public Color interface_color;
        public string interface_color_surrogate;
        public float interface_transparency;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">theme name</param>
        /// <param name="interface_color">theme color</param>
        /// <param name="text_color">theme text color</param>
        /// <param name="transparency">transparency value</param>
        public color_theme(string id, Color interface_color, float transparency)
        {
            this.id = id;
            this.interface_color = interface_color;
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
        /// <summary>
        /// Get theme color
        /// </summary>
        /// <returns>Color color</returns>
        public Color get_color()
        {
            return interface_color;
        }
        /// <summary>
        /// Update transparency value
        /// </summary>
        /// <param name="val"></param>
        public void set_transparency(float val)
        {
            interface_transparency = val;
        }
    }
    /// <summary>
    /// Editor class - for manipulation of the game world. addition and deletion of world tiles/props.
    /// </summary>
    [Serializable()]
    public class Editor
    {
// tools and various support collections
        [NonSerialized]private modes editor_mode;                                      // editor mode
        [NonSerialized]private tools editor_tools;                                     // editor tool
        [NonSerialized]private int submode_brush_radius;                               // this value tracks the size of submode brush used in combination with mode.add or mode.delete`. Ranges from 0 to 10
        [NonSerialized]private short edit_tile_id;                                     // which tile contexttype is currently highlighted
        [NonSerialized]private List<Vector2> selection_matrix;                         // list of all selected cells
        [NonSerialized]private List<PointLight> selection_lights;                      // list of all lights inside selection matrix
        [NonSerialized]private List<WaterGenerator> selection_watergen;                // list of all water generators inside selection matrix
        [NonSerialized]private List<Vector2> line_matrix;                              // list of all line cells - used to create new cells with line tool
        [NonSerialized]public GraphicInterface GUI;                                    // create a User Interface object in this host class       
        [NonSerialized]public Vector2 engine_offset = new Vector2(0, 0);               // placeholder vector for offsetting - draw tool text
// line tool and selection matrix   
        [NonSerialized] private Vector2? selection_start_cell, selection_end_cell;     // start/end cells in selection
        [NonSerialized] public Vector2 line_start_cell, line_end_cell;                 // line tool start/end points. If start cell exists - moving mouse makes program update current line status.Right click cancels start cell. Turn off context clicking while line start cell exists
// color theme parameters of the GUI
        [NonSerialized]private List<color_theme> themes = new List<color_theme>();
        private float sel_transparency;
        [NonSerialized] private Color selection_color;
        private string selection_color_surrogate;
        private color_theme current_theme;
        private float transparency = 1f;
// actions
        [NonSerialized]private actions? action;                                        // current editor action
        [NonSerialized]public bool gui_move_mode;                                      // gui element movement flag
        [NonSerialized]public bool locked;                                             // locked?
        [NonSerialized]private bool overwrite_cells;                                   // true = generate tile even if one exists in the cell, false = ignore existing ui_elements
        [NonSerialized]private bool editor_actions_locked;                             // true - clicking on world map won't change anything, false - tools are unlocked
// current text input functionality
        [NonSerialized]private TextInput current_focused;                              // since input can only be accepted by one target at a time - create a placeholder 
        [NonSerialized]private const int input_delay = 300;                            // amount of millisecond allowed before next keystroke is added to input string of current focused input
        [NonSerialized]private const int backspace_delay = 100;
        [NonSerialized]private long last_input_timestamp = 0;                          // when was the last input received
        [NonSerialized]private long last_backspace_timestamp = 0;                      // when was the last character erased

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public Editor(Engine engine)
        {
            editor_mode = modes.add;
            editor_tools = tools.radius;
            edit_tile_id = 2;
            submode_brush_radius = 0;                                                   // default = 0 cell radius - 1 cell currently pointed 
            selection_start_cell = selection_end_cell = null;                           // no cells selected by default
            line_start_cell = new Vector2(-1, -1);
            line_end_cell = new Vector2();
            selection_matrix = new List<Vector2>(1024);                                 // initialize selection matrix, expandable
            selection_lights = new List<PointLight>();
            selection_watergen = new List<WaterGenerator>();
            line_matrix = new List<Vector2>(512);
            GUI = new GraphicInterface(engine);                                         // initialize user interface
            locked = false;
            gui_move_mode = false;                                                      // UI can't be repositioned by default
            overwrite_cells = false;                                                    // editor will overwrite existing cell - default = false
            editor_actions_locked = false;
            selection_color = new Color(0, 175, 250);
            selection_color_surrogate = TextEngine.color_to_delimited_string(selection_color);

            // loading themes (contains a list of system defined colors)
            themes.Add(new color_theme("Black", Color.Black, transparency));
            themes.Add(new color_theme("Charcoal", new Color(45, 45, 45), transparency));
            themes.Add(new color_theme("Brown", new Color(204, 153, 26), transparency));
            themes.Add(new color_theme("Ultra Dark Red", new Color(94, 0, 0), transparency));
            themes.Add(new color_theme("Dark Red", Color.DarkRed, transparency));
            themes.Add(new color_theme("Crimson", Color.Crimson, transparency));
            themes.Add(new color_theme("Orange Red", Color.OrangeRed,  transparency));
            themes.Add(new color_theme("Orange", new Color(211, 117, 35), transparency));
            themes.Add(new color_theme("Ultra Dark Purple", new Color(61, 0, 94), transparency));
            themes.Add(new color_theme("Bright Purple", new Color(131, 17, 193), transparency));
            themes.Add(new color_theme("Dark Blue", new Color(0, 30, 200), transparency));
            themes.Add(new color_theme("Vibrant Blue", new Color(0, 128, 255),  transparency));
            themes.Add(new color_theme("Ultra Dark Green", new Color(62, 94, 0), transparency));
            current_theme = themes[0];
        }
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
            // Dynamically add PARTICLE MENU to the GUI
            // this will control the test particle object
            Container particle_menu = new Container("CONTAINER_PARTICLE_MENU", context_type.none, "particle control menu", new Vector2(300,300), true);
            SwitchButton<bool> enable_particles = new SwitchButton<bool>("PARTICLE_MENU_ENABLE_TEST", false, particle_menu, type.value_button_binarychoice, actions.enable_particle_test, confirm.no, new Rectangle(0, -1, 200, 30), null, "enable particle test", "turn on or off");
            particle_menu.add_element(enable_particles);

            InfoLabel particle_sector_separator = new InfoLabel("PARTICLE_MENU_SECTION1_SEPARATOR", particle_menu, type.info_label, actions.none, confirm.no, new Rectangle(0, -1, 200, 20), null, "particle shapes", "");
            particle_menu.add_element(particle_sector_separator);

            Button particle_star            = new Button("PARTICLE_MENU_STAR_TYPE", particle_menu, type.button, actions.change_test_particle_star, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: star", "changes particle type");
            Button particle_square          = new Button("PARTICLE_MENU_SQUARE_TYPE", particle_menu, type.button, actions.change_test_particle_square, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: square", "changes particle type");
            Button particle_circle          = new Button("PARTICLE_MENU_CIRCLE_TYPE", particle_menu, type.button, actions.change_test_particle_circle, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: circle", "changes particle type");
            Button particle_hollow_square   = new Button("PARTICLE_MENU_HSQUARE_TYPE", particle_menu, type.button, actions.change_test_particle_hollow_square, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: hollow square", "changes particle type");
            Button particle_raindrop        = new Button("PARTICLE_MENU_RAINDROP_TYPE", particle_menu, type.button, actions.change_test_particle_raindrop, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: raindrop", "changes particle type");
            Button particle_triangle        = new Button("PARTICLE_MENU_TRIANGLE_TYPE", particle_menu, type.button, actions.change_test_particle_triangle, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: triangle", "changes particle type");
            Button particle_x               = new Button("PARTICLE_MENU_X_TYPE", particle_menu, type.button, actions.change_test_particle_x, confirm.no, new Rectangle(0, -1, 200, 30), null, "particle: X", "changes particle type");
            particle_star.set_custom_label_positioning(orientation.vertical_left);
            particle_square.set_custom_label_positioning(orientation.vertical_left);
            particle_circle.set_custom_label_positioning(orientation.vertical_left);
            particle_hollow_square.set_custom_label_positioning(orientation.vertical_left);
            particle_raindrop.set_custom_label_positioning(orientation.vertical_left);
            particle_triangle.set_custom_label_positioning(orientation.vertical_left);
            particle_x.set_custom_label_positioning(orientation.vertical_left);

            particle_menu.add_element(particle_star);
            particle_menu.add_element(particle_square);
            particle_menu.add_element(particle_circle);
            particle_menu.add_element(particle_hollow_square);
            particle_menu.add_element(particle_raindrop);
            particle_menu.add_element(particle_triangle);
            particle_menu.add_element(particle_x);
            InfoLabel particle_mode_separator = new InfoLabel("PARTICLE_MENU_MODE_SEPARATOR", particle_menu, type.info_label, actions.none, confirm.no, new Rectangle(0, -1, 200, 20), null, "particle base trajectories", "");
            particle_menu.add_element(particle_mode_separator);
            Button particle_fall  = new Button("PARTICLE_MENU_FALL", particle_menu, type.button, actions.change_test_trajectory_fall, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: fall", "changes particle trajectory");
            Button particle_chaos = new Button("PARTICLE_MENU_CHAOS", particle_menu, type.button, actions.change_test_trajectory_chaos, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: chaos", "changes particle trajectory");
            Button particle_rise = new Button("PARTICLE_MENU_RISE", particle_menu, type.button, actions.change_test_trajectory_rise, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: rise", "changes particle trajectory");
            Button particle_ballistic = new Button("PARTICLE_MENU_BALLISTIC", particle_menu, type.button, actions.change_test_trajectory_ballistic_curve, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: ballistic", "changes particle trajectory");
            Button particle_static = new Button("PARTICLE_MENU_STATIC", particle_menu, type.button, actions.change_test_trajectory_static, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: static", "changes particle trajectory");
            Button particle_laser = new Button("PARTICLE_MENU_LASER", particle_menu, type.button, actions.change_test_trajectory_laser, confirm.no, new Rectangle(0, -1, 200, 20), null, "trajectory: laser", "changes particle trajectory");
            particle_fall.set_custom_label_positioning(orientation.vertical_left);
            particle_chaos.set_custom_label_positioning(orientation.vertical_left);
            particle_rise.set_custom_label_positioning(orientation.vertical_left);
            particle_ballistic.set_custom_label_positioning(orientation.vertical_left);
            particle_static.set_custom_label_positioning(orientation.vertical_left);
            particle_laser.set_custom_label_positioning(orientation.vertical_left);
            
            particle_menu.add_element(particle_fall);
            particle_menu.add_element(particle_chaos);
            particle_menu.add_element(particle_rise);
            particle_menu.add_element(particle_ballistic);
            particle_menu.add_element(particle_static);
            particle_menu.add_element(particle_laser);
            InfoLabel particle_separator = new InfoLabel("PARTICLE_MENU_COLOR_SEPARATOR", particle_menu, type.info_label, actions.none, confirm.no, new Rectangle(0, -1, 200, 20), null, "particle base color", "");
            particle_menu.add_element(particle_separator);
            Slider particle_base_colorR = new Slider("PARTICLE_COLOR_SLIDERR", particle_menu, type.slider, actions.change_test_particle_colorR, confirm.no, new Rectangle(0, -1, 200, 30), null, "red", "change particle color component");
            Slider particle_base_colorG = new Slider("PARTICLE_COLOR_SLIDERG", particle_menu, type.slider, actions.change_test_particle_colorG, confirm.no, new Rectangle(0, -1, 200, 30), null, "green", "change particle color component");
            Slider particle_base_colorB = new Slider("PARTICLE_COLOR_SLIDERB", particle_menu, type.slider, actions.change_test_particle_colorB, confirm.no, new Rectangle(0, -1, 200, 30), null, "blue", "change particle color component");
            particle_menu.add_element(particle_base_colorR);
            particle_menu.add_element(particle_base_colorG);
            particle_menu.add_element(particle_base_colorB);
            InfoLabel particle_lifetime_separator = new InfoLabel("PARTICLE_MENU_LIFETIME_SEPARATOR", particle_menu, type.info_label, actions.none, confirm.no, new Rectangle(0, -1, 200, 20), null, "particle properties", "");
            particle_menu.add_element(particle_lifetime_separator);
            Slider particle_lifetime_slider = new Slider("PARTICLE_LIFETIME_SLIDER", particle_menu, type.slider, actions.change_test_particle_lifetime, confirm.no, new Rectangle(0, -1, 200, 30), null, "lifetime", "change particle lifetime");
            Slider particle_rate_slider = new Slider("PARTICLE_CREATION_RATE_SLIDER", particle_menu, type.slider, actions.change_test_particle_creation_rate, confirm.no, new Rectangle(0, -1, 200, 30), null, "creation rate", "change particle generation rate");
            Slider particle_emitter_radius_slider = new Slider("PARTICLE_EMITTER_RADIUS_SLIDER", particle_menu, type.slider, actions.change_test_particle_emitter_radius, confirm.no, new Rectangle(0, -1, 200, 30), null, "emitter radius", "change particle emitter radius");
            Slider particle_burst_slider    = new Slider("PARTICLE_BURST_SLIDER", particle_menu, type.slider, actions.change_test_particle_burst, confirm.no, new Rectangle(0, -1, 200, 30), null, "burst amount", "change particle burst amount");
            Slider particle_scale_slider    = new Slider("PARTICLE_SCALE_SLIDER", particle_menu, type.slider, actions.change_test_particle_scale, confirm.no, new Rectangle(0, -1, 200, 30), null, "scale", "change particle scale");
            Slider particle_rotation_slider = new Slider("PARTICLE_ROTATION_SLIDER", particle_menu, type.slider, actions.change_test_particle_rotation, confirm.no, new Rectangle(0, -1, 200, 30), null, "rotation(radian)", "change particle rotation amount");
            particle_menu.add_element(particle_lifetime_slider);
            particle_menu.add_element(particle_rate_slider);
            particle_menu.add_element(particle_emitter_radius_slider);
            particle_menu.add_element(particle_burst_slider);
            particle_menu.add_element(particle_scale_slider);
            particle_menu.add_element(particle_rotation_slider);            
            InfoLabel particle_base_separator = new InfoLabel("PARTICLE_MENU_BASE_COLOR_SEPARATOR", particle_menu, type.info_label, actions.none, confirm.no, new Rectangle(0, -1, 200, 20), null, "particle secondary color", "");
            particle_menu.add_element(particle_base_separator);
            SwitchButton<bool> enable_color_interpolation = new SwitchButton<bool>("PARTICLE_MENU_ENABLE_COLOR_INTERPOLATION", false, particle_menu, type.value_button_binarychoice, actions.enable_particle_color_interpolation, confirm.no, new Rectangle(0, -1, 200, 30), null, "color interpolation", "turn on or off");
            particle_menu.add_element(enable_color_interpolation);

            GUI.add_container(particle_menu);

            // Dynamically add theme selection to context menu
            Container theme_container = GUI.find_container("CONTAINER_INTERFACE_COLOR_OPTIONS");// subcontext name for interface subcontext
            temp_id = "COLOR_THEMES_";
            counter = 0;
            // create interface theme updater
            Slider interface_transparency_slider = new Slider("INTERFACE_COLOR_TRANSPARENCY", theme_container, type.slider, actions.update_interface_transparency, confirm.no, new Rectangle(0, 0, 300, 30), null, "opaqueness %", "update interface transparency value");
            theme_container.add_element(interface_transparency_slider);

            foreach (color_theme t in themes)
            {
                IDButton<string> temp_cell = new IDButton<string>(temp_id + counter.ToString(), theme_container, type.id_button, actions.switch_theme, new Rectangle(0, (counter * 20) + 30, 300, 20), t.id, ""); // adjust rectangle for slider element
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
          
            GUI.create_UI_backgrounds();
            // load any custom items after GUI has been set up
            GUI.load_custom_element_background("MODE_BUTTON_ADDMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_DELETEMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_SELECTMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_LIGHTSMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_WATERMODE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("MODE_BUTTON_TREEMODE", engine.get_texture("200x30_background1"));

            GUI.load_custom_element_background("SUBMODE_BUTTON_RADIUS", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("SUBMODE_BUTTON_LINE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("SUBMODE_BUTTON_SQUARE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("SUBMODE_BUTTON_HOLLOW_SQUARE", engine.get_texture("200x30_background1"));

            GUI.load_custom_element_background("PARTICLE_MENU_STAR_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_SQUARE_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_CIRCLE_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_HSQUARE_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_TRIANGLE_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_RAINDROP_TYPE", engine.get_texture("200x30_background1"));
            GUI.load_custom_element_background("PARTICLE_MENU_X_TYPE", engine.get_texture("200x30_background1"));

            GUI.load_custom_element_background("PARTICLE_COLOR_SLIDERR", engine.get_texture("200x30_background2"));
            GUI.load_custom_element_background("PARTICLE_COLOR_SLIDERG", engine.get_texture("200x30_background2"));
            GUI.load_custom_element_background("PARTICLE_COLOR_SLIDERB", engine.get_texture("200x30_background2"));
                

            GUI.load_custom_container_background("CONTAINER_BRUSH_SIZE_OPTIONS", engine.get_texture("240x30_background1")); GUI.set_element_label_positioning("INFOLABEL_BRUSH_RADIUS", orientation.vertical_right);
            GUI.load_custom_container_background("CONTAINER_CURRENT_EDIT_CELL_PREVIEW", engine.get_texture("240x30_background1"));
            ((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).enable_button(false, engine.get_texture("editor_icon_locked"), engine.get_texture("editor_icon_unlocked"));
            GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON").set_icon(Tile.get_tile_struct(edit_tile_id).get_tile_icon_clip()); // 1042 - id of tile preview element
            // enable scrollbars if needed
            GUI.find_container("CONTAINER_EDITOR_TILE_CHANGER").enable_scrollbar(16, 6);

            // set world fill progress bar mask
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_mask(engine);
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_border(engine);
            ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_progress_color(Color.OrangeRed);

            // ((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_X")).set_progress_color(Color.SkyBlue);   // changing progress color
            //((ProgressCircle)GUI.find_element("CIRCLE_MOUSE_Y")).set_progress_color(Color.LimeGreen); // changing progress color
            ((TextInput)GUI.find_element("TEXTINPUT_SYSTEM")).set_input_target("TEXTAREA_SYSTEM");    // sets source for text area

            // set boundaries for system text area
            GUI.get_text_engine().set_target(((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_rectangle(), ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_origin());
            ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).set_border_texture(); // create border texture now that there is a bounds rectangle

            load_initial_slider_values(engine);
            // update statistics text area
            GUI.find_container("CONTAINER_STATISTICS_TEXT_AREA").set_transparency(0.25f);
        }
        /// <summary>
        /// Slider loading and data setting
        /// </summary>
        /// <param name="e">Engine instance</param>
        public void load_initial_slider_values(Engine e)
        {
            try
            {
                // editor tools
                ((Slider)GUI.find_unit(actions.update_selection_transparency)).set_slider_values(sel_transparency, 0.25f, 0.75f, 2);
                ((Slider)GUI.find_unit(actions.update_selection_color_red)).set_slider_values(selection_color.R, 0, 255);
                ((Slider)GUI.find_unit(actions.update_selection_color_green)).set_slider_values(selection_color.G, 0, 255);
                ((Slider)GUI.find_unit(actions.update_selection_color_blue)).set_slider_values(selection_color.B, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_grid_transparency)).set_slider_values(e.grid_transparency_value, 0.05f, 1.0f, 2);
                ((Slider)GUI.find_unit(actions.update_slider_grid_color_red)).set_slider_values(e.gridcolor_r, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_grid_color_green)).set_slider_values(e.gridcolor_g, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_grid_color_blue)).set_slider_values(e.gridcolor_b, 0, 255);
                ((Slider)GUI.find_unit(actions.update_brush_size)).set_slider_values(submode_brush_radius, 0, 10);
                // light context sliders
                ((Slider)GUI.find_unit(actions.update_slider_light_color_red)).set_slider_values(127, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_light_color_green)).set_slider_values(127, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_light_color_blue)).set_slider_values(127, 0, 255);
                ((Slider)GUI.find_unit(actions.update_slider_light_intensity)).set_slider_values(0.75f, 0.25f, 1.25f, 2);
                ((Slider)GUI.find_unit(actions.update_slider_light_range)).set_slider_values(1550, 100, 3000);               // max light range at 3000 pixels
                // particle color initial values
                ((Slider)GUI.find_unit(actions.change_test_particle_colorR)).set_slider_values(255, 0, 255);
                ((Slider)GUI.find_unit(actions.change_test_particle_colorG)).set_slider_values(0, 0, 255);
                ((Slider)GUI.find_unit(actions.change_test_particle_colorB)).set_slider_values(0, 0, 255);
                ((Slider)GUI.find_unit(actions.change_test_particle_lifetime)).set_slider_values(300, 50, 4000);             //lifetime value
                ((Slider)GUI.find_unit(actions.change_test_particle_emitter_radius)).set_slider_values(1, 1, 500);           //emitter radius
                ((Slider)GUI.find_unit(actions.change_test_particle_burst)).set_slider_values(1, 1, 30);                     //burst value
                ((Slider)GUI.find_unit(actions.change_test_particle_scale)).set_slider_values(1f, 0.1f, 2f, 1);              //scale value
                ((Slider)GUI.find_unit(actions.change_test_particle_rotation)).set_slider_values(0f, 0f, (float)Math.PI * 8);//rotation value in radians
                ((Slider)GUI.find_unit(actions.change_test_particle_creation_rate)).set_slider_values(1, 1, 1000);           //create every 1 ms or up to once in 1000ms
                ((Slider)GUI.find_unit(actions.update_interface_transparency)).set_slider_values(70, 25, 100);
                // SET SWITCH BUTTON VALUES
                ((SwitchButton<bool>)GUI.find_element("SWITCH_CLOCK_PAUSE_RESTART")).set_value(true); // clock is inactive by default
                ((SwitchButton<bool>)GUI.find_element("SWITCH_ENABLE_WORLD_LIGHTING")).set_value(true); // lighting is active by default
                ((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).enable_button(false, e.get_texture("editor_icon_locked"), e.get_texture("editor_icon_unlocked")); // locks ui movement by default
                // SET progress bars
                ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).set_element_values(0, 100, 0);
                ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).update((int)(e.get_current_world().get_percent_filled() * 100.0f));
            }
            catch(InvalidCastException ex)
            {
                Debug.WriteLine("cat failed: " + ex);
            }
        }

        /// <summary>
        /// Draws 1st layer of user interface
        /// </summary>
        /// <param name="spb">spritebatch used to draw</param>
        /// <param name="engine">Engine object</param>
        /// <param name="w">World for this editor object</param>
        public void draw_static_containers(SpriteBatch spb, Engine engine, World w)
        {
            // draw coordinate numbers
            for (int i = 0; i <= engine.get_current_world().width; i = i + 10)
            {
                int x_coor = engine.get_current_world().tilesize * (i - 1);
                int y_coor = -20;

                engine.xna_draw_outlined_text(i.ToString(),
                    new Vector2(x_coor, y_coor) - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
            }

            for (int i = 0; i <= engine.get_current_world().height; i = i + 10)
            {
                int x_coor = -20;
                int y_coor = engine.get_current_world().tilesize * (i - 1);

                engine.xna_draw_outlined_text(i.ToString(),
                    new Vector2(x_coor, y_coor) - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
            }
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
        /// Update Editor every frame
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void Update(Engine engine)
        {
            //UPDATE GUI ELEMENTS
            //GUI.set_interface_transparency(transparency);
            GUI.Update(engine);
            GUI.find_element("INFOLABEL_BRUSH_RADIUS").set_label("brush size  " + ((Slider)GUI.find_element("SLIDER_BRUSHSIZE")).get_slider_value_int().ToString());
            GUI.find_element("CURRENT_EDITOR_CELL_CHANGER_BUTTON").set_icon(Tile.get_tile_struct(edit_tile_id).get_tile_icon_clip());
            GUI.find_element("INFOLABEL_CURRENT_EDIT_TILE").set_label(Tile.get_tile_struct(edit_tile_id).get_name());
            ((ColorPreviewButton)GUI.find_element("PREVIEW_SELECTIONCOLOR")).update(selection_color);
            ((ColorPreviewButton)GUI.find_element("PREVIEW_EDITORGRID")).update(engine.get_grid_color());
            // update slider values connected to this editor 
            transparency = (float)GUI.get_slider_value(actions.update_interface_transparency)/100;
            for(int i = 0 ; i < themes.Count; i++)
            {
                themes[i].set_transparency(transparency);
            }
            GUI.set_interface_transparency(transparency);
            // selection values
            sel_transparency = GUI.get_slider_value(actions.update_selection_transparency);
            selection_color.R = (byte)GUI.get_slider_value(actions.update_selection_color_red);
            selection_color.G = (byte)GUI.get_slider_value(actions.update_selection_color_green);
            selection_color.B = (byte)GUI.get_slider_value(actions.update_selection_color_blue);
            // engine particle colors
            Color particle_color = new Color(0f, 0f, 0f, 1f);
            particle_color.R = (byte)GUI.get_slider_value(actions.change_test_particle_colorR);
            particle_color.G = (byte)GUI.get_slider_value(actions.change_test_particle_colorG);
            particle_color.B = (byte)GUI.get_slider_value(actions.change_test_particle_colorB);
            engine.change_particle_base_tint(particle_color);
            // particle sliders
            engine.change_particle_lifetime((int)GUI.get_slider_value(actions.change_test_particle_lifetime));
            engine.change_particle_emitter_radius((int)GUI.get_slider_value(actions.change_test_particle_emitter_radius));
            engine.change_particle_burst_amount((int)GUI.get_slider_value(actions.change_test_particle_burst));
            engine.change_particle_rotation_amount((float)GUI.get_slider_value(actions.change_test_particle_rotation));
            engine.change_particle_scale((float)GUI.get_slider_value(actions.change_test_particle_scale));
            engine.change_particle_creation_rate((int)GUI.get_slider_value(actions.change_test_particle_creation_rate));
            // brush values
            submode_brush_radius = (int)GUI.get_slider_value(actions.update_brush_size);
            // update choice values
            engine.enable_particle_test(GUI.get_tracked_value<bool>("PARTICLE_MENU_ENABLE_TEST")); // turn test particles on or off
            engine.enable_particle_color_interpolation(GUI.get_tracked_value<bool>("PARTICLE_MENU_ENABLE_COLOR_INTERPOLATION"), Color.Black); // turns interpolation on or off
            overwrite_cells = GUI.get_tracked_value<bool>("SWITCH_OVERWRITE_EXISTING_CELLS");
            engine.get_clock().set_paused(GUI.get_tracked_value<bool>("SWITCH_CLOCK_PAUSE_RESTART"));
            engine.set_lighting_state(GUI.get_tracked_value<bool>("SWITCH_ENABLE_WORLD_LIGHTING"));
            gui_move_mode = ((UIlocker)GUI.find_element("LOCKER_UI_MOVEMENT")).get_tracked_value(); // updates internal editor UI flag
            // update world percentage filled progress bar every half a second
            if (engine.get_frame_count() % 30 == 0) // limit update timing of this function
                ((ProgressBar)GUI.find_element("PROGRESS_BAR_WORLDFILL_PERCENTAGE")).update((int)(engine.get_current_world().get_percent_filled() * 100f));

            // set boundaries for text area
            GUI.get_text_engine().set_target(((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_rectangle(), ((TextArea)GUI.find_element("TEXTAREA_SYSTEM")).get_origin());

            // activate the button - visual
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
            else if (editor_mode == modes.water)
            {
                GUI.activate(modes.water);
            }
            else if (editor_mode == modes.prop_trees)
            {
                GUI.activate(modes.prop_trees);
            }
            // activate buttons for submode visual
            GUI.activate(editor_tools);
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
        /// <summary>
        /// Draw() function. Renders editor and its GUI
        /// </summary>
        /// <param name="spb">spritebatch used to draw</param>
        /// <param name="engine">Engine object</param>
        /// <param name="w">World for this editor object</param>
        public void Draw(SpriteBatch spb, Engine engine, World w)
        {
            // draw selection
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
                    int thickness = 1;
                    float color_factor = 0.55f; // darker borders
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
            // draw message for gui move mode
            if(gui_move_mode)
            {
                engine.xna_draw_outlined_text("UI Move Mode", new Vector2(engine.get_viewport().Width - Game1.large_font.MeasureString("UI Move Mode").X, 300), Vector2.Zero, Color.LightSkyBlue, Color.Black, Game1.large_font);
            }
            // Preview added/deleted cells if GUI is not hovered
            if ((editor_mode == modes.add || editor_mode == modes.delete) && !GUI.hover_detect() && !editor_actions_locked && !gui_move_mode)
            {
                if (editor_mode != modes.prop_trees)
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
            }
            if (editor_mode == modes.prop_trees)
                preview_tree(((int)engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).X), (int)engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine).Y, engine, engine.get_current_world().valid_tree_bases);

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
                    string line_length = "line width: " + line_matrix.Count.ToString();
                    engine.xna_draw_outlined_text(line_coordinates, engine.get_current_world().get_tile_origin(line_start_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    engine.xna_draw_outlined_text(line_coordinates2, engine.get_current_world().get_tile_origin(line_end_cell) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                    update_offset(0, 20); // draw line width below end cell
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

                if (editor_mode == modes.select)
                {
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[selection mode] " + selection_matrix_size().ToString() + " cells", engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if (editor_mode == modes.prop_lights)
                {
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[lights mode] add point light", engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.White, Color.Black, engine.get_UI_font());
                }
                else if(editor_mode == modes.water)
                {
                    int tile_id = engine.get_current_world().get_tile_id(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(),engine));
                    float water_content = engine.get_current_world().get_tile_water(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(),engine));
                   
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[water mode] id: "+tile_id+" water: "+water_content, engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, Color.DeepSkyBlue, Color.DarkSlateGray, engine.get_UI_font());
                }
                else if (editor_mode == modes.prop_trees)
                {
                    Vector2 cell = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine);
                    Color indicator = preview_tree((int)cell.X, (int)cell.Y, engine, engine.get_current_world().valid_tree_bases, true) == true ? Color.LawnGreen : Color.Red;
                    update_offset(0, -40);
                    engine.xna_draw_outlined_text("[tree mode] create a tree base", engine.get_current_world().get_tile_origin(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine)) + engine_offset - engine.get_camera_offset(), Vector2.Zero, indicator, Color.DarkSlateGray, engine.get_UI_font());
                }
                // draw line selection_start_cell coordinate at line start and draw line width
                // draw selection start cell coordinate, selection height, selection width and selection area
            }

            // draw text instructions
            engine.xna_draw_outlined_text("destroy: alt+W = water, alt+L = lights, alt+G = grass, alt+T = trees", new Vector2(40, 20), Vector2.Zero, Color.White,
                Color.DarkSlateGray, Game1.small_font);
            engine.xna_draw_outlined_text("TAB = toggle editor mode, F1 = toggle statistics display, F2 = change active world", new Vector2(40, 40), Vector2.Zero, Color.White,
                Color.DarkSlateGray, Game1.small_font);
            engine.xna_draw_outlined_text("in selection mode: insert/delete to add/remove tiles, alt+c - to change the tile type to currently active", new Vector2(40, 60), Vector2.Zero, Color.White,
               Color.DarkSlateGray, Game1.small_font);
            engine.xna_draw_outlined_text("~h = list of commands, ~a = list of actions", new Vector2(40, 100), Vector2.Zero, Color.White,
               Color.DarkSlateGray, Game1.small_font);
            engine.xna_draw_outlined_text("shift + escape - exit the program/close editor", new Vector2(40, 0), Vector2.Zero, Color.OrangeRed,
               Color.DarkSlateGray, Game1.small_font);
        }
        /// <summary>
        /// Draw masking layer. This layer hides any interface element parts that it covers. Masking is done in main game Draw through a shader.
        /// </summary>
        public void draw_masking_layer()
        {
            GUI.draw_masking_layer();
        }
        /// <summary>
        /// Draw borders, text and any parts of UI that must be on top layer.
        /// </summary>
        /// <param name="e">Engine instance</param>
        /// <param name="sb">Spritbatch for scheduling drawing order</param>
        public void draw_post_processing(Engine e, SpriteBatch sb)
        {
            if (e.get_editor().GUI.find_container("CONTAINER_EDITOR_TEXT_AREA").is_visible())
                GUI.get_text_engine().textengine_draw(e); // draw text stored inside text engine

            GUI.draw_post_processing(e, current_theme.interface_color, current_theme.interface_transparency);
        }
        /// <summary>
        /// Draw one or more ractengles in palce where future cells are either placed or removed.
        /// </summary>
        /// <param name="x">Position X</param>
        /// <param name="y">Position Y</param>
        /// <param name="engine">Engine instance</param>
        /// <param name="radius">brush tool radius</param>
        /// <param name="current">current editor mode</param>
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
        /// Preview cells
        /// </summary>
        /// <param name="x">Position X</param>
        /// <param name="y">Position Y</param>
        /// <param name="engine">Engine instance</param>
        /// <param name="current">current editor mode</param>
        /// <returns>true/false - tile can be placed in this cell</returns>
        public bool preview(int x, int y, Engine engine, modes current)
        {
            if (engine.get_current_world().tile_doesnt_exist(x, y) || cell_overwrite_mode() || current == modes.delete)
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
                    mode_color * Engine.fade_sine_wave_smooth(3000, 0.65f, 0.75f, sinewave.one), 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                }
            }

            return true;
        }
        /// <summary>
        /// Preview if tree can be placed in specified position.
        /// </summary>
        /// <param name="x">Position X</param>
        /// <param name="y">Position Y</param>
        /// <param name="engine">Engine instance</param>
        /// <param name="ground_type">an array of tiles that the given tree can be planted on</param>
        /// <param name="nodraw">Draw the preview rectangle or not</param>
        /// <returns></returns>
        public bool preview_tree(int x, int y, Engine engine, short[] ground_type, bool nodraw = false)
        {
            if 
            ( 
                engine.get_current_world().tile_doesnt_exist(x, y) 
                && y >= 5 // at least 5 tile away from the top edge
                && engine.get_current_world().tile_doesnt_exist(x, y - 1) // air
                && engine.get_current_world().tile_doesnt_exist(x, y - 2) // air
                && engine.get_current_world().tile_doesnt_exist(x, y - 3) // air
                && engine.get_current_world().trees.Find(t => engine.are_vectors_equal(t.get_position(), engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(),engine))) == null // no other tree exists here
                && Tree.preview(engine, new Vector2(x,y)) // check if at least 1 trunk segment can grow
            )
            {

                short hover_cell_id = engine.get_current_world().get_tile_id(new Vector2(x, y + 1)); // valid ground bases = defined by user

                if (engine.get_current_world().valid_cell(x, y) && ground_type.Contains(hover_cell_id))
                {
                    Rectangle current_cell_dimensions = engine.get_current_world().get_cell_rectangle_on_screen(engine, new Vector2(x, y));
                    Color mode_color = new Color();
                    mode_color = Color.LightGreen;

                    if(!nodraw)
                    engine.xna_draw(Engine.pixel,
                    engine.get_current_world().get_tile_origin(new Vector2(x, y)) - engine.get_camera_offset(),
                    Engine.standard20, // rectangle crop
                    mode_color * Engine.fade_sine_wave_smooth(3000, 0.65f, 0.75f, sinewave.one), 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Draws the preview cells
        /// </summary>
        /// <param name="engine">Engine instance</param>
        /// <param name="matrix">List of selected cells</param>
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
        /// <summary>
        /// Update engine offset (camera)
        /// </summary>
        /// <param name="x">position x</param>
        /// <param name="y">position y</param>
        public void update_offset(float x, float y)
        {
            engine_offset.X = x; engine_offset.Y = y;
        }
        /// <summary>
        /// Update brush tool radius
        /// </summary>
        /// <param name="val">new radius value</param>
        public void set_brush_size(int val)
        {
            if (val >= 0 || val <= 10)
            {
                ((Slider)GUI.find_unit(actions.update_brush_size)).set_slider_value(val);
            }
        }
        
// World Editor functions

        /// <summary>
        ///  Send a mouse keyboard input to editor class and execute an actions specified by the User Interface element clicked/used
        /// </summary>
        /// <param name="engine">Engine instance</param>
        /// <param name="c">command,e.g. left click</param>
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
            Vector2 active_cell = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine,false);

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
                case command.destroy_lights: // remove all point lights
                    engine.get_current_world().destroy_lights();
                    hide_expandable_containers_only();
                    break;
                case command.destroy_trees: // remove all point lights
                    engine.get_current_world().destroy_trees();
                    hide_expandable_containers_only();
                    break;
                case command.destroy_grass: // remove all point lights
                    engine.get_current_world().destroy_grass();
                    hide_expandable_containers_only();
                    break;
                case command.destroy_water_gen: // remove all point lights
                    engine.get_current_world().destroy_water_generators();
                    engine.get_current_world().reset_water();
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
                        // submit the text to output handler
                        if (current_focused != null && current_focused.get_text().Length > 0) // make sure there is a text input active
                        {// then send text info to text area and erase current data in the text input
                            //((TextArea)GUI.find_element(current_focused.get_input_target_id())).accept_text_output(current_focused.get_text()); // send input
                            GUI.get_text_engine().add_message_element(engine, "system[0,75,220] ( ~time ): " + current_focused.get_text()); // adding a coded system word to input to mark message source
                            current_focused.clear_text();
                        }
                        // unfocus the main input - only if there is no text
                        else if (current_focused != null && current_focused.get_text().Length == 0) 
                        {
                            if (current_focused != null)
                            {
                                current_focused.set_focus(false);
                                current_focused = null;
                            }
                        }
                        // focus main input if Enter is pressed and there is no focused element
                        else
                        {
                            current_focused = (TextInput)GUI.find_element("TEXTINPUT_SYSTEM"); // assign main input 
                            current_focused.set_focus(true); // mark element focused internally
                        }
                    }
                    break;
                // new functionality: tile creation/deletion for the selection mode
                case command.delete_key:
                    {
                        // if world editor is in selection mode, delete key can be used to remove a selected group of cells 
                        if (editor_mode == modes.select)
                        {
                            // delete cells (but only if no lights in the selection matrix - lights are deleted first!)
                            if (selection_lights.Count == 0 && selection_watergen.Count == 0)
                            {
                                foreach (Vector2 v in selection_matrix)
                                {
                                    engine.get_current_world().erase_tile((int)v.X, (int)v.Y, engine); // remove tiles contained in the selection matrix
                                }
                            }

                            // delete lights
                            foreach (PointLight p in selection_lights)
                            {
                                if (engine.get_current_world().world_lights.Contains(p))
                                {
                                    engine.get_current_world().world_lights.Remove(p); // delete this light from world objects
                                }
                            }
                            // delete water generators
                            foreach (WaterGenerator w in selection_watergen)
                            {
                                if (engine.get_current_world().wsources.Contains(w))
                                {
                                    engine.get_current_world().wsources.Remove(w); // delete this 
                                }
                            }
                        }
                    }
                    break;
                case command.insert_key:
                    {
                        // if world editor is in selection mode, delete key can be used to remove a selected group of cells 
                        if (editor_mode == modes.select)
                        {
                            foreach (Vector2 v in selection_matrix)
                            {
                                engine.get_current_world().generate_tile(get_current_editor_cell(), (int)v.X, (int)v.Y, engine); // create tiles of currently selected type inside the selection
                            }
                        }
                    }
                    break;
                case command.alt_c:
                    {
                        // if world editor is in selection mode, delete key can be used to remove a selected group of cells 
                        if (editor_mode == modes.select)
                        {
                            foreach (Vector2 v in selection_matrix)
                            {
                                if (engine.get_current_world().tile_exists(v)) // only change for existing tiles
                                    engine.get_current_world().update_tile_type((int)v.X, (int)v.Y, engine);
                            }
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
                                    }
                                    else if (c == command.right_click || c == command.right_hold)
                                    {
                                        if (editor_tools == tools.line && !editor_actions_locked)
                                        {
                                            line_matrix.Clear();
                                            line_start_cell.X = -1; // -1 will be treated as null by the driver function
                                            line_start_cell.Y = -1;                                                
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
                                            selection_end_cell = (engine.are_vectors_equal(active_cell,new Vector2(-1,-1)))? Vector2.One : active_cell;
                                    }
                                }
                                    break;
                                case modes.prop_lights:
                                {
                                    clear_selection();
                                    if (c == command.left_click && !editor_actions_locked)
                                    {
                                        engine.get_current_world().generate_light_source(new Color(Engine.generate_int_range(0, 255), Engine.generate_int_range(0, 255), Engine.generate_int_range(0, 255)), engine.get_current_mouse_state(), engine, Engine.generate_int_range(300, 800), Engine.generate_float_range(0.15f, 1.35f));
                                    }
                                }
                                    break;
                                case modes.water: 
                                {
                                    clear_selection();
                                    if (c == command.left_click && !editor_actions_locked)
                                    {
                                        engine.get_current_world().generate_water_generator(engine.get_current_mouse_state(), engine);
                                    }
                                }
                                break;
                                case modes.prop_trees:
                                {
                                    clear_selection();
                                    if (c == command.left_click && !editor_actions_locked)
                                    {
                                        engine.get_current_world().generate_tree_base(engine.get_current_mouse_state(), engine);
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
                                case command.ctrl_plus_click:
                                    {
                                        // build selection matrix by clicking once and then ctrl+clicking the end cell instead of dragging
                                        selection_matrix.Add(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(),engine));
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
                    case actions.editor_mode_switch_water:
                        {
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_selection();
                            clear_line_mode();

                            switch_mode("water");
                        }
                        break;
                    case actions.editor_mode_switch_tree:
                        {
                            hide_all_contexts();
                            hide_expandable_containers_only();
                            clear_selection();
                            clear_line_mode();

                            switch_mode("tree");
                        }
                        break;
                    case actions.editor_submode_switch_radius:
                        {
                            editor_tools = tools.radius;
                        }
                        break;
                    case actions.editor_submode_switch_line:
                        {
                            editor_tools = tools.line;
                        }
                        break;
                    case actions.editor_submode_switch_square:
                        {
                            editor_tools = tools.square;
                        }
                        break;
                    case actions.editor_submode_switch_hollow_square:
                        {
                            editor_tools = tools.hollow_square;
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
                        engine.getGame1().update_resolution(1920, 1080);
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
                        engine.set_camera_offset(new Vector2(-engine.get_viewport().Width / 2, -engine.get_viewport().Height / 2)); // reset camera offset
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
//---------------------------------------------------------------------------------------------------------------Sliders
                    case actions.update_interface_transparency:
                        { slider_driver(engine, c, actions.update_interface_transparency); }
                        break;
                    case actions.update_slider_grid_transparency:
                        { slider_driver(engine, c, actions.update_slider_grid_transparency); }
                        break;
                    case actions.update_slider_grid_color_red:
                        { slider_driver(engine, c, actions.update_slider_grid_color_red); }
                        break;
                    case actions.update_slider_grid_color_green:
                        { slider_driver(engine, c, actions.update_slider_grid_color_green); }
                        break;
                    case actions.update_slider_grid_color_blue:
                        { slider_driver(engine, c, actions.update_slider_grid_color_blue); }
                        break;
                    case actions.update_selection_transparency:
                        { slider_driver(engine, c, actions.update_selection_transparency); }
                        break;
                    case actions.update_selection_color_red:
                        { slider_driver(engine, c, actions.update_selection_color_red); }
                        break;
                    case actions.update_selection_color_green:
                        { slider_driver(engine, c, actions.update_selection_color_green); }
                        break;
                    case actions.update_selection_color_blue:
                        { slider_driver(engine, c, actions.update_selection_color_blue); }
                        break;
                    case actions.update_slider_light_color_red: // new
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_light_color_red)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_light_color_red), engine.get_mouse_vector());
                                    // also update the currently hovered light(s)
                                    if(get_selection_lights().Count > 0)
                                    {
                                        foreach(PointLight p in get_selection_lights())
                                        {
                                            int r = ((Slider)GUI.find_unit(actions.update_slider_light_color_red)).get_slider_value_int();
                                            int g = p.get_color().G;
                                            int b = p.get_color().B;

                                            // find the light in the same position as the one in selection matrix. then change it's color
                                            engine.get_current_world().world_lights.Find(x => x.position == p.position).change_light_color(new Color(r, g, b)); 
                                        }
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_slider_light_color_green: // new
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_light_color_green)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_light_color_green), engine.get_mouse_vector());
                                    // also update the currently hovered light(s)
                                    if (get_selection_lights().Count > 0)
                                    {
                                        foreach (PointLight p in get_selection_lights())
                                        {
                                            int r = p.get_color().R;
                                            int g = ((Slider)GUI.find_unit(actions.update_slider_light_color_green)).get_slider_value_int();
                                            int b = p.get_color().B;

                                            // find the light in the same position as the one in selection matrix. then change it's color
                                            engine.get_current_world().world_lights.Find(x => x.position == p.position).change_light_color(new Color(r, g, b)); 
                                        }
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_slider_light_color_blue: // new
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_light_color_blue)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_light_color_blue), engine.get_mouse_vector());
                                    // also update the currently hovered light(s)
                                    if (get_selection_lights().Count > 0)
                                    {
                                        foreach (PointLight p in get_selection_lights())
                                        {
                                            int r = p.get_color().R;
                                            int g = p.get_color().G;
                                            int b = ((Slider)GUI.find_unit(actions.update_slider_light_color_blue)).get_slider_value_int();

                                            // find the light in the same position as the one in selection matrix. then change it's color
                                            engine.get_current_world().world_lights.Find(x => x.position == p.position).change_light_color(new Color(r, g, b)); 
                                        }
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_slider_light_intensity: // new
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_light_intensity)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_light_intensity), engine.get_mouse_vector());
                                    // also update the currently hovered light(s)
                                    if (get_selection_lights().Count > 0)
                                    {
                                        foreach (PointLight p in get_selection_lights())
                                        {
                                            float i = ((Slider)GUI.find_unit(actions.update_slider_light_intensity)).get_slider_value();
                                            // find the light in the same position as the one in selection matrix. then change it's color
                                            engine.get_current_world().world_lights.Find(x => x.position == p.position).change_intensity(i);
                                        }
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.update_slider_light_range: // new
                        {
                            set_hostUI_lock(true);
                            if (GUI.get_hovered_element_action_enum() == actions.update_slider_light_range)
                            {
                                // change the slider value
                                if (c == command.left_hold)
                                {
                                    GUI.update_slider_values(GUI.find_unit(actions.update_slider_light_range), engine.get_mouse_vector());
                                    // also update the currently hovered light(s)
                                    if (get_selection_lights().Count > 0)
                                    {
                                        foreach (PointLight p in get_selection_lights())
                                        {
                                            int i = ((Slider)GUI.find_unit(actions.update_slider_light_range)).get_slider_value_int();
                                            // find the light in the same position as the one in selection matrix. then change it's color
                                            engine.get_current_world().world_lights.Find(x => x.position == p.position).change_range(i);
                                        }
                                    }
                                }
                                else
                                    set_hostUI_lock(false);
                            }
                        }
                        break;
                    case actions.change_test_particle_colorR: // new
                        { slider_driver(engine, c, actions.change_test_particle_colorR); }
                        break;
                    case actions.change_test_particle_colorG: // new
                        { slider_driver(engine, c, actions.change_test_particle_colorG); }
                        break;
                    case actions.change_test_particle_colorB: // new
                        { slider_driver(engine, c, actions.change_test_particle_colorB); }
                        break;
                    case actions.change_test_particle_lifetime: // new
                        { slider_driver(engine, c, actions.change_test_particle_lifetime); }
                        break;
                    case actions.change_test_particle_burst: // new
                        { slider_driver(engine, c, actions.change_test_particle_burst); }
                        break;
                    case actions.change_test_particle_emitter_radius: // new
                        { slider_driver(engine, c, actions.change_test_particle_emitter_radius); }
                        break;
                    case actions.change_test_particle_scale: // new
                        { slider_driver(engine, c, actions.change_test_particle_scale); }
                        break;
                    case actions.change_test_particle_rotation: // new
                        { slider_driver(engine, c, actions.change_test_particle_rotation); }
                        break;
                    case actions.change_test_particle_creation_rate: // new
                        { slider_driver(engine, c, actions.change_test_particle_creation_rate); }
                        break;
//---------------------------------------------------------------------------------------------------------------Sliders End
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
                            if (line_start_cell.X == -1) // if line exists - right click should cancel it instead of context
                            {
                                hide_all_contexts(); // close all other menus before opening this one again

                                // show light context if light is hovered or if there are lights in selection
                                if (
                                    engine.get_current_world().is_light_object_in_cell(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine,false))
                                    || get_selection_lights().Count > 0 
                                    )
                                {
                                    // new context menu will be here
                                    GUI.change_container_origin(engine.get_viewport(), "lights context menu", engine.get_mouse_vector() + Vector2.One);

                                    if (c == command.right_click)
                                    {
                                        GUI.set_container_visibility("lights context menu", true);
                                        //if light is hovered - add the matching light from all lights (by position)
                                        if(engine.get_current_world().is_light_object_in_cell(engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine, false)))
                                        {
                                            editor_mode = modes.select; // switch mode
                                            selection_start_cell = engine.get_current_world().get_current_hovered_cell(engine.get_current_mouse_state(), engine, false);
                                            selection_end_cell = selection_start_cell;
                                        }
                                    }
                                }
                                else // show main context if no light is hovered
                                {
                                    GUI.change_container_origin(engine.get_viewport(), "context menu", engine.get_mouse_vector() + Vector2.One); // create almost at mouse, to avoid inital hover

                                    if (c == command.right_click) // ignore if something is in line tool start cell
                                        GUI.set_container_visibility("context menu", true);
                                }
                                // lock editor tools
                                editor_actions_locked = true;
                            }
                        }
                        break;
                    // switch buttons update
                    case actions.option_value_switch:
                    case actions.enable_particle_test:
                    case actions.enable_particle_color_interpolation:
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
                    //------------------------------
                    case actions.change_cell_design:
                        {
                            if (GUI.get_hovered_element() is IDButton<short> && c == command.left_click)
                            {
                                edit_tile_id = ((IDButton<short>)GUI.get_hovered_element()).get_tracked_value();
                                hide_all_contexts();
                            }
                        }
                        break;
                    case actions.change_test_particle_star:
                        engine.change_test_particle_shape(particle_type.star);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_square:
                        engine.change_test_particle_shape(particle_type.square);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_circle:
                        engine.change_test_particle_shape(particle_type.circle);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_hollow_square:
                        engine.change_test_particle_shape(particle_type.hollow_square);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_raindrop:
                        engine.change_test_particle_shape(particle_type.raindrop);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_triangle:
                        engine.change_test_particle_shape(particle_type.triangle);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_particle_x:
                        engine.change_test_particle_shape(particle_type.x);
                        GUI.activate_particle_test_buttons((actions)action);
                        break;
                    case actions.change_test_trajectory_fall:
                        engine.change_particle_trajectory(trajectory_type.fall);
                        break;
                    case actions.change_test_trajectory_chaos:
                        engine.change_particle_trajectory(trajectory_type.chaotic);
                        break;
                    case actions.change_test_trajectory_rise:
                        engine.change_particle_trajectory(trajectory_type.rise);
                        break;
                    case actions.change_test_trajectory_ballistic_curve:
                        engine.change_particle_trajectory(trajectory_type.ballistic_curve);
                        break;
                    case actions.change_test_trajectory_static:
                        engine.change_particle_trajectory(trajectory_type.static_);
                        break;
                    case actions.change_test_trajectory_laser:
                        engine.change_particle_trajectory(trajectory_type.laser_line);
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
                    case actions.toggle_system_chat:
                        {
                            if (c == command.left_click)
                            {
                                GUI.find_container("CONTAINER_EDITOR_TEXT_AREA").set_visibility("toggle");
                                GUI.find_container("CONTAINER_MAIN_TEXT_INPUT").set_visibility("toggle");

                                if(current_focused != null)
                                {
                                    current_focused.set_focus(false);
                                    current_focused.clear_text();
                                    current_focused = null;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }//end of action switch statement
            }//end of move mode independent section
        }//end of function

// support functions 

        /// <summary>
        /// New: Handles slider GUI
        /// </summary>
        /// <param name="engine">engine object for mouse properties</param>
        /// <param name="c">command e.g. left click/hold</param>
        /// <param name="act">action e.g. actions.change_test_particle_emitter_radius</param>
        public void slider_driver(Engine engine, command c, actions act)
        {
            set_hostUI_lock(true);
            if (GUI.get_hovered_element_action_enum() == act)
            {
                // change the slider value
                if (c == command.left_hold)
                {
                    GUI.update_slider_values(GUI.find_unit(act), engine.get_mouse_vector());
                }
                else
                    set_hostUI_lock(false);
            }
        }
        /// <summary>
        /// Editor will accept a keyboard input as characters if there is an active text input target 
        /// </summary>
        /// <param name="value">string input</param>
        public void accept_input(Engine engine, string value)
        {
            if (current_focused == null)
                return;

            long current = Engine.get_current_game_millisecond();
            string last = current_focused.get_last_input();

            if (!String.Equals(last, value)) // different input
            {
                if (current_focused != null)
                {
                    current_focused.add_text(value);
                    last_input_timestamp = current; // assign new value to most recent input
                }
            }
            else // same input
            {
                if (last_input_timestamp + input_delay <= current)
                {
                    if (current_focused != null)
                    {
                        current_focused.add_text(value);
                        last_input_timestamp = Engine.get_current_game_millisecond(); // assign new value to most recent input
                    }
                }
            }
        }
        /// <summary>
        /// Get a list of Point Lights currently selected.
        /// </summary>
        /// <returns>List of lights</returns>
        public List<PointLight> get_selection_lights()
        {
            return selection_lights;
        }
        /// <summary>
        /// Get input status
        /// </summary>
        /// <returns>true or false</returns>
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
        /// <summary>
        /// Update millisecond of last key input
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void refresh_focused_input_delay(Engine engine)
        {
            last_input_timestamp = Engine.get_current_game_millisecond(); // to keep delay active in case there was no input
        }
        /// <summary>
        /// Backspace handler
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void erase_one_character_from_input(Engine engine)
        {
            if ((last_backspace_timestamp + backspace_delay <= Engine.get_current_game_millisecond()))
            {
                current_focused.erase_one_character();
                last_backspace_timestamp = Engine.get_current_game_millisecond();
            }
        }
        /// <summary>
        /// Get a TextInput object that is the current input
        /// </summary>
        /// <returns>TextInput object</returns>
        public TextInput get_current_input_target()
        {
            return current_focused;
        }
        /// <summary>
        /// Find theme by name
        /// </summary>
        /// <param name="id">name id</param>
        /// <returns>color theme if found, if not return a default</returns>
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
        /// <summary>
        /// Get current color theme
        /// </summary>
        /// <returns>color theme</returns>
        public color_theme get_current_theme()
        {
            return current_theme;
        }
        /// <summary>
        /// Selection transparency value
        /// </summary>
        /// <returns>transparency value 0-1</returns>
        public float get_selection_transparency()
        {
            return sel_transparency;
        }
        /// <summary>
        /// Get selection color
        /// </summary>
        /// <returns>selection color value</returns>
        public Color get_selection_color()
        {
            return selection_color;
        }
// functions executed by "editor_command"
        /// <summary>
        /// Deletes entire world cells
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void delete_all_cells(Engine engine)
        {
            for (int i = 1; i <= engine.get_current_world().width; i++)
            {
                for (int j = 1; j <= engine.get_current_world().height; j++)
                {
                    engine.get_current_world().erase_tile(i, j, engine);
                }
            }
        }
        /// <summary>
        /// Fill all available cells with a tile block
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void fill_all_cells(Engine engine)
        {
            for (int i = 1; i <= engine.get_current_world().width; i++)
            {
                for (int j = 1; j <= engine.get_current_world().height; j++)
                {
                    if (!engine.get_current_world().tile_exists(new Vector2(i, j)))
                        engine.get_current_world().generate_tile(edit_tile_id, i, j, engine);
                }
            }
        }
        /// <summary>
        /// Change submode
        /// </summary>
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
        /// <summary>
        /// Change submode
        /// </summary>
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
        /// <summary>
        /// Get overwrite existing cells status
        /// </summary>
        /// <returns>true or false - can overwrite</returns>
        public bool cell_overwrite_mode()
        {
            return overwrite_cells;
        }
        /// <summary>
        /// Update overwrite cells mode
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_overwrite_mode(bool value)
        {
            overwrite_cells = value;
        }
        /// <summary>
        /// Return the id of currently active cell type. This cell type will be added to an empty cell if a tool is used. 
        /// This cell type will also be updated to if selection matrix is active and alt+c is pressed
        /// </summary>
        /// <returns>tile id</returns>
        public short get_current_editor_cell()
        {
            return edit_tile_id;
        }
        /// <summary>
        /// Convert selection cells from cell address to world_map array indexes
        /// </summary>
        /// <returns>array of 2 vectors - start and end cells</returns>
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
        /// <summary>
        /// Update current editor mode
        /// </summary>
        /// <param name="mode">new mode name</param>
        public void switch_mode(String mode)
        {
            if (mode == "add")
            {
                editor_mode = modes.add;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(true);
            }
            else if (mode == "delete")
            {
                editor_mode = modes.delete;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(true);
            }
            else if (mode == "select")
            {
                editor_mode = modes.select;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(false);
            }
            else if (mode == "lights")
            {
                editor_mode = modes.prop_lights;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(false);
            }
            else if (mode == "water")
            {
                editor_mode = modes.water;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(false);
            }
            else if (mode == "tree")
            {
                editor_mode = modes.prop_trees;
                GUI.find_container("CONTAINER_SUBMODES").set_visibility(false);
            }
        }
        /// <summary>
        /// Hide all containers that have a context property activated
        /// </summary>
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
        /// <summary>
        /// Hide all context containers 
        /// </summary>
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
        /// <summary>
        /// Remove cells from the line tool matrix. Does not physically remove cells from the world.
        /// </summary>
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
        /// <summary>
        /// Handles line tool. Creates a line based on mouse movement.
        /// </summary>
        /// <param name="engine">Engine instance</param>
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
        /// <summary>
        /// Serialization of selection color
        /// </summary>
        /// <returns>string value of color</returns>
        public string get_selection_color_surrogate()
        {
            return selection_color_surrogate;
        }
        /// <summary>
        /// Get real value of interface color
        /// </summary>
        /// <returns>current theme color</returns>
        public Color get_interface_color()
        {
            return current_theme.interface_color;
        }
        /// <summary>
        /// Get interface transparency value
        /// </summary>
        /// <returns>float value 0-1</returns>
        public float get_interface_transparency()
        {
            return current_theme.interface_transparency;
        }
        /// <summary>
        /// Number of selection matrix cells
        /// </summary>
        /// <returns>int count</returns>
        public int selection_matrix_size()
        {
            return selection_matrix.Count;
        }
        /// <summary>
        /// Clear cells from current selection (does not remove them from the world)
        /// </summary>
        public void clear_selection()
        {
            selection_start_cell = null;
            selection_end_cell = null;
            selection_matrix.Clear();
            selection_lights.Clear();
            selection_watergen.Clear();
        }

        /// <summary>
        /// Removes focus from all the inputs
        /// </summary>
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
                if(current_focused != null)
                   current_focused.set_focus(false);

                current_focused = null;       // clear focus from input
            }
        }

        /// <summary>
        /// selection matrix handling function
        /// </summary>
        /// <param name="engine">Engine instance</param>
        public void selection_driver(Engine engine)
        {
            // calculate if selection is in a proper range
            // validity of cells calculated in caller functions
            if (selection_start_cell == null || selection_end_cell == null)
                return;

            Vector2 beginning = (Vector2)selection_start_cell;
            Vector2 end       = (Vector2)selection_end_cell;

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

            // add world object to selection lists
            selection_lights.Clear(); // prepare for refresh
            foreach (PointLight p in engine.get_current_world().world_lights)
            {
                // adjust for negative values, since world starts at 1 
                Vector2 a = engine.get_current_world().vector_position_to_cell(p.position);
                if(a.X < 0 && a.Y < 0)
                {
                    a -= Vector2.One;
                }
                else if(a.X < 0)
                {
                    a -= new Vector2(1, 0);
                }
                else if (a.Y < 0)
                {
                    a -= new Vector2(0, 1);
                }
                //engine.get_current_world().vector_position_to_cell(p.position);// if selection matrix contains
                if (selection_matrix.Contains(a))
                {
                    selection_lights.Add(p); // add this light to selection lights
                }
            }

            // add water gen to selection matrix
            selection_watergen.Clear();
            foreach (WaterGenerator w in engine.get_current_world().wsources)
            {
                //engine.get_current_world().vector_position_to_cell(p.position);// if selection matrix contains
                if (selection_matrix.Contains(engine.get_current_world().vector_position_to_cell(w.get_position())))
                {
                    selection_watergen.Add(w); // add this light to selection lights
                }
            }
        }
        /// <summary>
        /// Deserialize interface 
        /// </summary>
        /// <param name="deserialized_list">list of containers saved in an xml file</param>
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
            catch (NullReferenceException)
            {
                // no user interface info saved - use defaults
            }
        }
        /// <summary>
        /// Set the state of newly created elements
        /// </summary>
        /// <param name="deserialized_editor">Editor instance</param>
        public void seed_interface_with_color_data(Editor deserialized_editor)
        {
            if (deserialized_editor == null)
                return;

            current_theme = deserialized_editor.get_current_theme();
            current_theme.deserialize_color_string();
            sel_transparency = 0.5f;

            // attempt to resurrect selection color data
            try
            {
                selection_color = TextEngine.delimited_string_to_color(deserialized_editor.get_selection_color_surrogate());
            }
            catch (NullReferenceException)
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
