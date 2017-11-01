using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{
    /// <summary>
    /// GraphicInterface is an overall GUI containing all containers/stored_elements. Also manages GUI related actions
    /// Structure: GUI < Container < Unit < Internal Unit Zones
    /// </summary>
    public class GraphicInterface : IEngine
    {
        private List<Container> containers;                         // list of containers                
        private Container hovered_container;                        // assign hovered references
        private UIElementBase hovered_unit;
        private actions?[] action_list = new actions?[2];           // hierarchy of currently available actions
        private bool visible;                                       // is GUI currently visible or hidden
        private bool locked;                                        // a GUI function is being executed - other actions locked
        private bool outline_containers;
        private const short OUTLINE_THICKNESS = 2;                  // outline for containers when in move mode
        private TextEngine textengine;

        private Engine _engine;
        public Engine engine
        {
            get { return _engine; }
            set { _engine = value; }
        }
        // parametreless constructor for serialization
        public GraphicInterface()
        {

        }
        /// <summary>
        /// GUI constructor initiates a List of containers and assigns currently hovered elements. Also makes GUI visible/hidden
        /// </summary>
        public GraphicInterface(Engine engine)
        {
            containers = new List<Container>();
            hovered_container = null;
            hovered_unit = null;
            visible = true;
            locked = false;
            outline_containers = false;
            this.engine = engine;

            textengine = new TextEngine(engine.get_UI_font(), Rectangle.Empty, Vector2.Zero, Color.White); // creating a textengine inside GUI
        }
        // GUI Functions-----------------------------------------------------------------------------------------------------------------------135c
        public TextEngine get_text_engine()
        {
            return textengine;
        }
        public bool is_locked()
        {
            return locked;
        }
        public void set_lock_state(bool state)
        {
            locked = state;
        }

        public bool are_containers_outlined()
        {
            return outline_containers;
        }

        public void set_container_outline(bool value)
        {
            outline_containers = value;
        }
        /// <summary>
        /// Get actual user interface contents
        /// </summary>
        /// <returns>User interface contents and their inner elements (including all parameters)</returns>
        public List<Container> get_all_containers()
        {
            return containers;
        }

        public UIElementBase get_hovered_element()
        {
            return hovered_unit;
        }

        public Container get_hovered_container()
        {
            return hovered_container;
        }
        /// <summary>
        /// Create all GUI element background based on bounds. All background are created with a White color, then element color is assigned in Draw() function
        /// </summary>
        public void create_UI_backgrounds(/*Engine engine*/)
        {
            foreach (Container c in containers)
            {
                // create container background first only if rectangle exists (should always exist)
                // Color.White is used for every background. element color is added during rendering by Color Tint parameter
                if (c.get_rectangle() != null)
                {
                    if (c.get_rectangle().Width != 0 && c.get_rectangle().Height != 0) // must be defined. Rectangle has 0 width/height only if it contains nothing. therefore it doesn't need background
                    {
                        Texture2D temp_bg = Game1.create_colored_rectangle(c.get_rectangle(), Color.White, 1f);
                        c.add_background_texture(temp_bg);
                    }
                }
                // create background for each element next
                if (c.get_element_list().Count > 0)
                {
                    foreach (UIElementBase e in c.get_element_list())
                    {
                        // generate a Unit background
                        Texture2D temp_bg = Game1.create_colored_rectangle(e.get_rectangle(), Color.White, 1f);
                        e.add_background_texture(temp_bg);
                        // generate a tooltip background
                        Vector2 tooltip_dimensions = engine.get_UI_font().MeasureString(e.get_tooltip_text()); // display in GUI font
                        Texture2D temp_t = Game1.create_colored_rectangle(new Rectangle(0, 0, (int)tooltip_dimensions.X + 10, (int)tooltip_dimensions.Y + 4), Color.DimGray, 1f); // additional pixels are needed for aesthetics 
                        e.add_tooltip_background(temp_t);
                        // specific by different GUI element 
                        if (e is Button)
                        {
                            Button temp = (Button)e;

                        }
                        else if (e is Slider)
                        {
                            Slider temp = (Slider)e;
                            Texture2D temp_slider = Game1.create_colored_rectangle(new Rectangle(0, 0, 3, 16), Color.DeepSkyBlue, 0.8f);
                            temp.add_slider_background(engine.get_texture("item-sliderline"));
                            temp.add_slider(temp_slider);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// All necessary updates, e.g hovered status etc.
        /// </summary>
        /// <param name="engine">Engine object reference</param>
        /// <param name="editor_mode">Current editor mode inside WorldEditor</param>
        public void Update(Engine engine)
        {
            // update hovered component information and assign references. Update internal component hover states as well
            hover_detect();

            foreach (Container c in containers)
            {
                c.update();
                // test Container for being outside the viewport
                c.isOutsideBounds(engine.get_viewport());

                // bool window_collision_h = false; // refresh this only once per container tested
                //bool window_collision_v = false;
                // test Container for collision with other containers
                if (c.get_bounds().X != 0)
                {
                    foreach (Container cc in containers)
                    {
                        // don't take contexts into account and don't compare container to itself, don't compare to gui locker button either
                        if (cc == c || c.get_context_type() != context_type.none || cc.get_context_type() != context_type.none
                            || c.get_id().Equals("CONTAINER_GUI_MOVE_LOCKER") || cc.get_id().Equals("CONTAINER_GUI_MOVE_LOCKER"))
                        {
                            continue;
                        }
                        else
                        {
                            /*while (cc.get_bounds().Intersects(c.get_bounds()))
                            {
                                // special case - if at any point container container intersects window borders - change direction of the movement
                                if (c.get_bounds().X <= 0 || c.get_bounds().X >= engine.getGame1().viewport.Width)                                 
                                {
                                    window_collision_h = true;
                                }

                                if(c.get_bounds().Y <= 0 || c.get_bounds().Y >= engine.getGame1().viewport.Height)
                                {
                                    window_collision_v = true;
                                }
                                // test borders
                                if (c.get_bounds().Left <= cc.get_bounds().Left) // current container intersects on the left border
                                {
                                    if (c.get_bounds().Top <= cc.get_bounds().Top) // intersection on the top border
                                    {
                                        // move left and up
                                        c.de_intersect(new Vector2(-1, -1), window_collision_h,window_collision_v);
                                    }
                                    else // intersection on the bottom border
                                    {
                                        // move left and down
                                        c.de_intersect(new Vector2(-1, 1), window_collision_h,window_collision_v);
                                    }
                                }
                                else // intersects on right border
                                {
                                    if (c.get_bounds().Top <= cc.get_bounds().Top) // intersection on the top border
                                    {
                                        // move right and up
                                        c.de_intersect(new Vector2(1, -1), window_collision_h,window_collision_v);
                                    }
                                    else // intersection on the bottom border
                                    {
                                        // move right and down
                                        c.de_intersect(new Vector2(1, 1), window_collision_h,window_collision_v);
                                    }
                                }
                            }*/
                        }
                    }
                }//if
            }

            textengine.set_font(engine.get_UI_font());
            textengine.update(); // recalculates wrapped word lines
        }
        /// <summary>
        /// Update unit states based on current states. Also assigns hover beginning 
        /// </summary>
        /// <param name="engine">Engine object reference</param>
        public void update_all_component_states(/*Engine engine*/)
        {
            foreach (Container c in containers)
            {// set container states
                if (c.get_rectangle().Intersects(engine.get_mouse_rectangle()) && c.is_visible())
                {
                    c.set_state(state.hovered);
                }
                else
                {
                    c.set_state(state.default_state);
                }
                // update stored_elements
                foreach (UIElementBase e in c.get_element_list())
                {
                    Rectangle real_rectangle = new Rectangle(c.get_rectangle().X + e.get_rectangle().X, c.get_rectangle().Y + e.get_rectangle().Y, e.get_rectangle().Width, e.get_rectangle().Height);

                    if (real_rectangle.Intersects(engine.get_mouse_rectangle()))
                    {
                        if (e.get_state() != state.hovered) // if not currently hovered - assign hovered state and start time
                        {
                            e.set_hover_start_time(engine.get_current_game_millisecond());
                            e.set_state(state.hovered);
                        }
                    }
                    else
                        e.set_state(state.default_state);
                }
            }
        }
        /// <summary>
        /// Overall draw() - display all stored_elements and their states + any stats info if available
        /// </summary>
        /// <param name="s">SpriteBatch used to draw</param>
        /// <param name="engine">Engine object reference</param>
        public void draw_static_containers(SpriteBatch s, Color color, float transparency)
        {
            // ignore if GUI is invisible
            if (visible == false)
                return;
            // draw all containers and elements inside
            // only pick those containers that are defined as non-context elements 
            foreach (Container c in containers.Where(element => (element.get_context_type() == context_type.none)))
            {
                if (c.is_visible() == false || c.get_rectangle().Width == 0 || c.get_rectangle().Height == 0 || c.get_element_list().Count == 0) // if container is invisible - skip, also skip if Container is empty
                    continue;
                // draw container
                c.draw(engine, color); // also draws all of it'engine elements based on internal container rules               
            }
            // draw scrollbars separately
            // only pick those containers that are defined as non-context elements 
            foreach (Container c in containers.Where(element => (element.get_context_type() == context_type.none)))
            {
                // draw scrollbar
                if (c.scrollbar_enabled() && c.is_visible())
                    c.draw_scrollbar(engine, transparency);

                if (c.is_visible() == false || c.get_state() != state.hovered) // if container is invisible - skip, if not hovered - skip tooltip rendering
                    continue;
            }
        }

        public void draw_context_containers_and_tooltips(SpriteBatch s, Color color, float transparency)
        {
            // ignore if GUI is invisible
            if (visible == false)
                return;
            // draw all containers and elements inside
            // only pick those containers that are defined as non-context elements 
            foreach (Container c in containers.Where(element => (element.get_context_type() != context_type.none)))
            {
                if (c.is_visible() == false || c.get_rectangle().Width == 0 || c.get_rectangle().Height == 0 || c.get_element_list().Count == 0) // if container is invisible - skip, also skip if Container is empty
                    continue;
                // draw container
                c.draw(engine, color); // also draws all of it'engine elements based on internal container rules
                // draw outline on context containers if in move mode
                if (outline_containers)
                {
                    if (!c.get_id().Equals("CONTAINER_GUI_MOVE_LOCKER"))
                        engine.xna_draw_rectangle_outline(c.get_rectangle(), Color.LightSkyBlue, OUTLINE_THICKNESS);
                }
            }
            // draw scrollbars separately
            // only pick those containers that are defined as non-context elements 
            foreach (Container c in containers.Where(element => (element.get_context_type() != context_type.none)))
            {
                // draw scrollbar
                if (c.scrollbar_enabled() && c.is_visible())
                    c.draw_scrollbar(engine, transparency);

                if (c.is_visible() == false || c.get_state() != state.hovered) // if container is invisible - skip, if not hovered - skip tooltip rendering
                    continue;
            }
            // include all tooltips in this sequence - reason: post processing overwrites context containers and tooltips based on the order of drawing
            foreach (Container c in containers)
                foreach (UIElementBase e in c.get_element_list())
                {
                    // draw tooltips if element and container are visible
                    if(e.is_visible() && c.is_visible())
                        e.draw_tooltip(engine);
                }
        }

        public void draw_masking_layer()
        {
            // ignore if GUI is invisible
            if (visible == false)
                return;
            // draw all containers
            foreach (Container c in containers)
            {
                if (c.is_visible() == false || c.get_rectangle().Width == 0 || c.get_rectangle().Height == 0 || c.get_element_list().Count == 0) // if container is invisible - skip, also skip if Container is empty
                    continue;
                // draw masking sprites of any interface element
                foreach (UIElementBase e in c.get_element_list())
                {
                    e.draw_masking_sprite(engine);
                }
            }
        }
        public void draw_post_processing(Engine ee, Color interface_color, float interface_transparency)
        {
            // ignore if GUI is invisible
            if (visible == false)
                return;
            // draw all containers
            foreach (Container c in containers)
            {
                if (c.is_visible() == false || c.get_rectangle().Width == 0 || c.get_rectangle().Height == 0 || c.get_element_list().Count == 0) // if container is invisible - skip, also skip if Container is empty
                    continue;
                // draw masking sprites of any interface element
                foreach (UIElementBase e in c.get_element_list())
                {
                    e.draw_post_processing(ee, interface_color, interface_transparency);
                    // post processing - draws outline for all containers in move mode (flag updated by editor class hosting a GUI object for that GUI object only
                    if (outline_containers)
                    {
                        if (!c.get_id().Equals("CONTAINER_GUI_MOVE_LOCKER"))
                            engine.xna_draw_rectangle_outline(c.get_rectangle(), Color.LightSkyBlue, OUTLINE_THICKNESS);
                    }

                    // draw tooltips
                    e.draw_tooltip(engine);
                }
            }
        }
        /// <summary>
        /// Detects hovered on any GUI element and assigns names for hovered container/unit
        /// </summary>
        /// <param name="engine">Engine object reference</param>
        /// <returns>true/false value representing GUI hover</returns>
        public bool hover_detect()
        {
            if (locked) // testing - if GUI is locked - don't update hover detection until lock is removed
                return true;

            hovered_container = null;
            hovered_unit = null;
            // detect hovered container 

            // update states 
            update_all_component_states(/*engine*/);
            foreach (Container c in containers)
            {
                if (c.get_rectangle().Intersects(engine.get_mouse_rectangle()) 
                    && c.is_visible())
                {// container is hovered
                    hovered_container = c; // assume that no containers are intersecting
                    action_list[0] = c.get_action();

                    foreach (UIElementBase e in c.get_element_list())
                    {
                        // calculate element rectangle by adding container origin
                        Rectangle real_rectangle = new Rectangle(c.get_rectangle().X + e.get_rectangle().X, c.get_rectangle().Y + e.get_rectangle().Y, e.get_rectangle().Width, e.get_rectangle().Height);

                        if (real_rectangle.Intersects(engine.get_mouse_rectangle()) && e.is_visible())
                        {// element is hovered
                            hovered_unit = e;
                            action_list[1] = e.get_action();
                            return true;
                        }
                    }
                    return true; // this code is reached if a container is hovered and even if no elements is hovered
                }
            }
            return false;
        }
        /// <summary>
        /// Get name of the hovered unit to select an action
        /// </summary>
        /// <returns>String value of the hovered unit'engine name</returns>
        public actions? get_hovered_element_action_enum()
        {
            if (hovered_unit != null)
                return hovered_unit.get_action();
            else return null;
        }
        public String get_hovered_element_action_text()
        {
            if (hovered_unit != null)
                return hovered_unit.get_action().ToString();
            else return null;
        }
        public string get_hovered_container_id()
        {
            if (hovered_container != null)
                return hovered_container.get_id();
            else return "none";
        }

        public string get_hovered_element_id()
        {
            if (hovered_unit != null)
                return hovered_unit.get_id();
            else return null;
        }
        /// <summary>
        /// For an editor_command - perform an action associated with a hovered element.
        /// Ui specific action handled inside GUI. Other action should be forwarded back to calling class.
        /// </summary>
        /// <param name="c">command enum value</param>
        /// <returns>action that will be performed</returns>
        public actions? ui_command(command c)
        {
            actions? current = null;

            if (hovered_unit != null || hovered_container != null) // hover exists
            {
                switch (c)
                {
                    case command.left_click:
                        current = get_current_action();
                        break;
                    case command.left_hold:
                        current = get_current_action();
                        break;
                    case command.left_release:
                        current = get_current_action();
                        break;
                    default:
                        current = null;
                        break;
                }
            }
            else // no hover
            {
                switch (c)
                {
                    case command.left_click: // clicking outside GUI - if context menu is active send an action to Editor host class
                        {
                            if (find_container("context menu", true) != null)
                            {
                                if (find_container("context menu", true).is_visible())
                                    current = actions.hide_menu;
                                else
                                    return null;
                            }
                        }
                        break;
                    case command.right_click:
                        current = actions.overall_context; // this action does not depend on any hover, hover must not exist
                        break;
                    default:
                        break;
                }
            }

            return current;
        }
        /// <summary>
        /// In action list if action[1] is null return action[0] - element precedes container action.
        /// Used in ui_command()
        /// </summary>
        /// <returns>action enum value that will be performed</returns>
        public actions? get_current_action()
        {
            if (action_list[1] == null)
            {
                return action_list[0];
            }
            else
            {
                return action_list[1];
            }
        }
        /// <summary>
        /// Activate buttons - show active indicators if a button is related to a state of host class
        /// </summary>
        /// <param name="m">modes enum parameter found in editor class</param>
        public void activate(modes m)
        {
            // inactivate all to reset
            foreach (Container c in containers)
            {
                foreach (UIElementBase u in c.get_element_list())
                {
                    u.inactivate();
                }
            }
            // activate one
            switch (m)
            {
                case modes.add:
                    if (find_unit(actions.editor_mode_switch_add) != null)
                    {
                        find_unit(actions.editor_mode_switch_add).activate();
                    }
                    break;
                case modes.delete:
                    if (find_unit(actions.editor_mode_switch_delete) != null)
                    {
                        find_unit(actions.editor_mode_switch_delete).activate();
                    }
                    break;
                case modes.select:
                    if (find_unit(actions.editor_mode_switch_select) != null)
                    {
                        find_unit(actions.editor_mode_switch_select).activate();
                    }
                    break;
                case modes.prop_lights:
                    if (find_unit(actions.editor_mode_switch_lights) != null)
                    {
                        find_unit(actions.editor_mode_switch_lights).activate();
                    }
                    break;
                case modes.water:
                    if (find_unit(actions.editor_mode_switch_water) != null)
                    {
                        find_unit(actions.editor_mode_switch_water).activate();
                    }
                    break;
                case modes.prop_trees:
                    if (find_unit(actions.editor_mode_switch_tree) != null)
                    {
                        find_unit(actions.editor_mode_switch_tree).activate();
                    }
                    break;
                default:
                    break;
            }
        }

        public void activate(tools t)
        {   // reset
            find_unit(actions.editor_submode_switch_radius).inactivate();
            find_unit(actions.editor_submode_switch_line).inactivate();
            find_unit(actions.editor_submode_switch_square).inactivate();
            find_unit(actions.editor_submode_switch_hollow_square).inactivate();

            // calculate current active
            if(t == tools.radius)
            {
                find_unit(actions.editor_submode_switch_radius).activate();
            }
            else if (t == tools.line)
            {
                find_unit(actions.editor_submode_switch_line).activate();
            }
            else if (t == tools.square)
            {
                find_unit(actions.editor_submode_switch_square).activate();
            }
            else if (t == tools.hollow_square)
            {
                find_unit(actions.editor_submode_switch_hollow_square).activate();
            }
        }
        /*public List<UIElementBase> get_all_elements()
        {
            return elements;
        }*/
        /// <summary>
        /// Function used to search for a GUI element during assignment phase
        /// </summary>
        /// <param name="id">id number of an element being searched</param>
        /// <returns></returns>
        public UIElementBase find_element(string id)
        {
            foreach (Container c in containers)
            {
                foreach (UIElementBase u in c.get_element_list())
                {
                    if (u.get_id() == id) // find specified element
                    {
                        return u;
                    }
                }
            }
            return null;
        }

        public UIElementBase find_element(Container parent, actions a)
        {
            return parent.find_element(a);
        }
        /// <summary>
        /// Find container by name. Can be used to find certain container from outside classes.
        /// </summary>
        /// <param name="target_name">Container'engine name</param>
        /// <returns>Container object if found or null</returns>
        public Container find_container(String target_name, bool dummy)
        {
            foreach (Container c in containers)
            {
                if (c.get_name() == target_name) // find specified element
                {
                    return c;
                }
            }
            return null;
        }
        public Container find_container(string target_id)
        {
            foreach (Container c in containers)
            {
                if (c.get_id() == target_id) // find specified element
                {
                    return c;
                }
            }
            return null;
        }
        /// <summary>
        /// Find a unit by action assigned to it. Can be used to find certain container from outside classes.
        /// </summary>
        /// <param name="action_name">action enum name</param>
        /// <returns>UIelement object if found otherwise null</returns>
        public UIElementBase find_unit(actions action_name)
        {
            foreach (Container c in containers)
            {
                foreach (UIElementBase e in c.get_element_list())
                {
                    if (e.get_action() == action_name) // find specified element
                    {
                        return e;
                    }
                }
            }
            return null;
        }
        /*public UIElementBase find_unit_by_tooltip(String tooltip)
        {
            foreach (Container c in containers)
            {
                foreach (UIElementBase e in c.get_element_list())
                {
                    if (e.get_tooltip_text() == tooltip) // find specified element
                    {
                        return e;
                    }
                }
            }
            return null;
        }*/
        public UIElementBase find_unit(String label)
        {
            foreach (Container c in containers)
            {
                foreach (UIElementBase e in c.get_element_list())
                {
                    if (e.get_label() == label) // find specified element
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        public List<Container> get_containers()
        {
            return containers;
        }
        /// <summary>
        /// Makes Container visible/hidden
        /// </summary>
        /// <param name="target_name">Container name</param>
        /// <param name="value">true/false - visible/hidden</param>
        public void set_container_visibility(/*Engine engine, */String target_name, bool value) // add a fade in option - all elements appear at a different time
        {
            foreach (Container c in containers)
            {
                if (c.get_name() == target_name) // find specified element
                {
                    if (value == true)
                        c.set_container_fade_start(engine.get_current_game_millisecond());

                    c.set_visibility(value);
                }
            }
        }
        /// <summary>
        /// Access Unit'engine slider value
        /// </summary>
        /// <param name="target_name">Unit name</param>
        /// <returns>float value associated with a slider</returns>
        public float get_slider_value(actions a_name)
        {
            float temp = 0.0f; // temp placeholder 

            foreach (Container c in containers)
            {
                foreach (UIElementBase e in c.get_element_list())
                {
                    if (e.get_action() == a_name) // find specified element
                    {
                        if (e is Slider)
                        {
                            Slider tmp = (Slider)e;
                            temp = tmp.get_slider_value();
                        }
                    }
                }
            }

            return temp;
        }
        /// <summary>
        /// Change slider value by holding left mouse button
        /// </summary>
        /// <param name="e">Unit'engine name</param>
        /// <param name="mouse">Mouse position</param>
        public void update_slider_values(UIElementBase unit, Vector2 mouse)
        {
            foreach (Container c in containers)
            {
                foreach (UIElementBase u in c.get_element_list())
                {
                    if (u == unit) // find specified element
                    {
                        // update slider value based on mouse movement
                        if (u is Slider)
                        {
                            Slider tmp = (Slider)u;
                            tmp.update(mouse);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Add container to GUI.
        /// </summary>
        /// <param name="c">Container reference</param>
        public void add_container(Container c)
        {
            containers.Add(c);
        }
        /// <summary>
        /// Add element to a list. During GUI construction elements from this list will be assigned to their containers etc.
        /// </summary>
        /// <param name="engine">GUI element</param>
        /*public void add_element_temporary(UIElementBase engine)
        {
            elements.Add(engine);
        }*/
        /// <summary>
        /// Change container position:origin. Called from the GUI by this wrapper function which in turn calls a container function with the same purpose.
        /// Usage: for a context menu - whenever a context menu is summoned - it has to appear at the mouse coordinates
        /// </summary>
        /// <param name="target_container">Container'engine name</param>
        /// <param name="new_position">Position at which Container should be drawn</param>
        /// <returns>bool value of success</returns>
        public void change_container_origin(Viewport v, String target_container, Vector2 mouse_position)
        {
            foreach (Container c in containers)
            {
                if (c.get_name() == target_container) // find specified element
                {
                    int x, y = 0; // coordinate placeholders
                    // calculate y coordinate based on viewport, container size and mouse position
                    if (mouse_position.Y + c.get_rectangle().Height < v.Height) // is within viewport
                        y = (int)mouse_position.Y;
                    else
                        y = v.Height - c.get_rectangle().Height;
                    // calculate x coordinate based on the same rules
                    if (mouse_position.X + c.get_rectangle().Width < v.Width) // is within viewport
                        x = (int)mouse_position.X;
                    else
                        x = v.Width - c.get_rectangle().Width;

                    // set new origin
                    c.set_origin(new Vector2(x, y));
                }
            }
        }
        /// <summary>
        /// Sets position where sub-context menus appear on screen. 
        /// Requires main context menu position on screen and viewport bounds
        /// </summary>
        /// <param name="subcontext"></param>
        /// <param name="new_origin"></param>
        public void update_sub_context_origin(Container subcontext, Viewport v, Vector2 suggested_position)
        {
            // find context container stats
            Rectangle context_size = find_container("context menu", true).get_rectangle();

            if (subcontext.scrollbar_enabled())
            {
                context_size.Height += 30; // adjust for ghost elements ~15px both ends
                context_size.Y -= 15;
            }
            // calculate
            int x, y = 0; // coordinate placeholders
            // calculate y coordinate based on viewport, container size and container position
            if (suggested_position.Y + subcontext.get_rectangle().Height < v.Height) // is within viewport
                y = (int)suggested_position.Y;
            else
                y = (int)(suggested_position.Y - subcontext.get_rectangle().Height);
            // calculate x coordinate based on the same rules
            if (suggested_position.X + subcontext.get_rectangle().Width < v.Width) // is within viewport
                x = (int)suggested_position.X + 1; //+1 to leave 1 px gap between context and subcontext
            else
                x = (int)(suggested_position.X - subcontext.get_rectangle().Width - context_size.Width) - 1; // put on the other side of original context? -1 to leave 1 px gap between context and subcontext
            // set new origin
            subcontext.set_origin(new Vector2(x, y));
        }
        /// <summary>
        /// Load custom background for element from GUI class
        /// </summary>
        /// <param name="element_id"></param>
        /// <param name="bg"></param>
        public void load_custom_element_background(string element_id, Texture2D bg)
        {
            find_element(element_id).load_custom_background(bg);
        }

        public void load_custom_container_background(string container_id, Texture2D bg)
        {
            find_container(container_id).add_custom_container_background(bg); // also sets all elements to 0.0f scale
        }

        public void set_interface_transparency(float value)
        {
            foreach (Container c in containers)
            {
                c.set_transparency(value);

                foreach (UIElementBase u in c.get_element_list())
                {
                    u.set_transparency(value);
                }
            }
        }

        public void set_element_label_positioning(string id, orientation value)
        {
            find_element(id).set_custom_label_positioning(value);
        }
        /// <summary>
        /// Get tracked value
        /// </summary>
        /// <typeparam name="T">contexttype of value</typeparam>
        /// <param name="element_id">id of GUI element</param>
        /// <returns></returns>
        public T get_tracked_value<T>(string element_id)
        {

            if (find_element(element_id) is SwitchButton<T>)
            {
                SwitchButton<T> temp = (SwitchButton<T>)find_element(element_id);
                return (T)temp.get_tracked_value();
            }

            return default(T);
        }
    }// class end
}// namespace end
