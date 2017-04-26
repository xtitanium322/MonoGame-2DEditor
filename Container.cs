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

namespace beta_windows
{
    /// <summary>
    /// container is a part of GUI. Container hold GUI stored_elements such as buttons, sliders, selectors etc.
    /// </summary>
    [Serializable()]
    public class Container
    {
        private string id;
        [NonSerialized]
        private Rectangle bounds;
        private string bounds_surrogate; // a serializeable format for Rectangle value - saves container position if moved
        [NonSerialized]
        private Texture2D background;
        [NonSerialized]
        private Color bg_color;
        private float bg_transparency;
        private String container_name; // not a label
        private List<UIElementBase> stored_elements; // all GUI stored_elements inside of this container
        private bool visible; // used to show/hide separate containers
        private bool customized_background;
        private actions? click_action; // null or action; if null - element is for display only, otherwise clicking on this element will initiate execution of some "action" by GUI "editor_command" function
        private state state;
        private context_type contexttype;

        private bool scrollbar;      // scrollbar mode is active or inactive
        private int scrollbar_width;        // width of the scrollbar - will be added to context positioning formula if scrollbar bool is true
        private int visible_elements;    // number of elements that are allowed in current view, for example, 5 out of total 15
        private int current_top_element;    // on top of the visible list of elements in container
        private const int minimal_top_element = 1;
        private int scroll_slider_height;   // stored variable - height of scroll slider as it is now
        private int max_y_start; // furthest position of the slider possible with current number of elements and slider size
        private int current_y_start; // current slider coordinate
        // container fade in effect values
        private long fade_in_start_time;
        private float fade_in_delay = 0f;
        private float fade_in_duration = 0f; // 250 ms test
        private const float fade_in_step = 0f; // difference between consecutive elements assigned fade_in_start_time
        // Container for GUI stored_elements-------------------------------------------------------------------------------------------------------------135c
        public Container()
        {
        }
        /// <summary>
        /// 2nd Constructor
        /// </summary>
        /// <param name="name">Container name</param>
        /// <param name="origin">Starting position on screen</param>
        /// <param name="visible">Is this Container visible or hidden</param>
        public Container(string id, context_type t, String name, Vector2 origin, bool visible)
        {
            this.id = id;
            contexttype = t;
            stored_elements = new List<UIElementBase>(); // create a list for stored_elements
            background = null; // initialize without a background
            bounds = new Rectangle((int)origin.X, (int)origin.Y, 0, 0);
            bg_color = Color.Transparent;
            bg_transparency = 1f;
            container_name = name;
            click_action = null;
            this.visible = visible;
            state = state.default_state;
            customized_background = false;
            // default scroll mode variables for container
            this.scrollbar = false;
            scrollbar_width = 0;
            visible_elements = 0; // won't be used until enabled anyway
            current_top_element = 1;
            scroll_slider_height = 0;
            max_y_start = 0;
            current_y_start = 0;

            fade_in_start_time = 0;
        }

        public void enable_scrollbar(int scrollbar_width, int visible_elements)
        {
            this.scrollbar_width = scrollbar_width;
            this.visible_elements = visible_elements;
            scrollbar = true;
            this.bounds.Width += scrollbar_width;
            recalculate_bounds(); // adjust container size based on number of currently visible elements in order
        }
        // component for main Draw functions
        // limit number of elements displayed
        public void draw_scrollbar(Engine engine, float transparency)
        {
            if (scrollbar)
            {
                float val = 0;
                if (fade_in_start_time != 0)
                {
                    val = engine.fade_up(fade_in_start_time, fade_in_delay, fade_in_duration + 200, 1.0f);
                }
                // calculate rectangles for drawing
                Rectangle scroll_area = new Rectangle(this.bounds.Width + this.bounds.X, this.bounds.Y, scrollbar_width, this.bounds.Height);
                Rectangle slider = new Rectangle(this.bounds.Width + this.bounds.X, current_y_start, scrollbar_width, scroll_slider_height);
                // draw tx_pixel in these 2 areas
                engine.xna_draw(Engine.pixel, new Vector2(scroll_area.X - slider.Width, scroll_area.Y), scroll_area, Color.Black * (bg_transparency * val), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                engine.xna_draw(Engine.pixel, new Vector2(scroll_area.X - slider.Width, slider.Y + scroll_area.Y), slider, Color.DimGray * (bg_transparency * val), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            }
        }
        /// <summary>
        /// Recalculate Container height and each elements bounds.Y coordinate
        /// </summary>
        public void recalculate_bounds()
        {
            this.bounds.Height = 0; // reset

            foreach (UIElementBase u in stored_elements)
            {
                if (u.is_visible()) // depending on visibility - container will be adjusted
                {
                    u.modify_bounds(u.get_rectangle().X, this.bounds.Height, u.get_rectangle().Width, u.get_rectangle().Height); // only adjust y coordinates of elementsfor vertical scrolled list
                    this.bounds.Height += u.get_rectangle().Height;
                }
            }
            // create a serializeable surrogate for Rectangle type
            bounds_surrogate = Engine.rectangle_to_delimited_string(bounds);
        }
        /// <summary>
        /// Update function for scrollbars
        /// </summary>
        public void update_scrolled_container()
        {
            if (scrollbar) // elements visibility phase
            {
                // update slider variables
                scroll_slider_height = (int)((float)this.bounds.Height * ((float)visible_elements / (float)this.stored_elements.Count));
                max_y_start = this.bounds.Y + (this.bounds.Height - scroll_slider_height);
                current_y_start = (int)(((float)max_y_start - (float)this.bounds.Y) * (((float)current_top_element - (float)minimal_top_element) / ((float)(this.stored_elements.Count - visible_elements + 1) - (float)minimal_top_element)));
                // activate/deactivate elements         
                int counter = 1;
                foreach (UIElementBase u in stored_elements)
                {
                    if (counter < current_top_element || counter > (current_top_element + visible_elements - 1))
                    {
                        u.set_visible(false);
                    }
                    else
                    {
                        u.set_visible(true);
                    }
                    counter++;
                }

                // based on current top_element - recalculate Container bounds (scrollbar variables will be calculated dynamically)
                // also move draw_scrollbar contained update code down here
                recalculate_bounds(); // elements above have been updated - recalculate container boundaries
            }
        }
        /// <summary>
        /// Scrollbar function up
        /// </summary>
        public void scroll_up()
        {
            if (current_top_element != (this.stored_elements.Count - visible_elements + 1))
                current_top_element++;
        }
        /// <summary>
        /// Scrollbar function down
        /// </summary>
        public void scroll_down()
        {
            if (current_top_element != minimal_top_element)
                current_top_element--;
        }
        /// <summary>
        /// Set the Container current_state - hovered/default etc.
        /// </summary>
        /// <param name="c">current_state enum value e.g. hovered</param>
        public void set_state(state c)
        {
            state = c;
        }
        /// <summary>
        /// Get container boundaries
        /// </summary>
        /// <returns>Rectangle dimension</returns>
        public Rectangle get_bounds()
        {
            return bounds;
        }
        /// <summary>
        /// Get current current_state enum value of Container
        /// </summary>
        /// <returns>current current_state enum value e.g. hovered</returns>
        public state get_state()
        {
            return state;
        }
        /// <summary>
        /// A string representation of a bounds Rectangle
        /// </summary>
        /// <returns>comma delimited string</returns>
        public string get_bounds_surrogate()
        {
            return bounds_surrogate;
        }
        /// <summary>
        /// Recreates bounds rectangle based on surrogate
        /// </summary>
        public void deserialize_rectangle_surrogate_string()
        {
            bounds = Engine.delimited_string_to_rectangle(bounds_surrogate);
        }
        public string get_id()
        {
            return id;
        }
        public context_type get_context_type()
        {
            return contexttype;
        }
        /// <summary>
        /// Detects hover of the element (Unused)
        /// </summary>
        /// <returns>bool value of the hover</returns>
        public bool detect_hover()
        {
            return false;
        }
        /// <summary>
        /// Updates Container if needed
        /// </summary>
        public void update()
        {
            update_scrolled_container();
            // refresh a serializeable surrogate for Rectangle type
            bounds_surrogate = Engine.rectangle_to_delimited_string(bounds);
        }

        /// <summary>
        /// This function checks Element for being outside the viewport and moves it back inside borders if needed
        /// Only done for visible non context containers
        /// </summary>
        /// <param name="v">viewport object</param>
        public void isOutsideBounds(Viewport v)
        {
          if (this.contexttype == context_type.none && this.is_visible())
            {
                // test viewport and dimensions using:
                //      this.bounds.Width;
                //      this.bounds.Height;
                //      this.bounds.X;
                //      this.bounds.Y;
                // 1st case - element is beyond right border
                while (v.Width < this.bounds.X + this.bounds.Width)
                {
                    this.bounds.X -= 1; // move to the left until the condition is no longer applicable
                }

                // 2nd case - element is beyond lower border
                while (v.Height < this.bounds.Y + this.bounds.Height)
                {
                    this.bounds.Y -= 1;
                }

                // move back into the screen - horizontal
                if (this.bounds.X < 0)
                    this.bounds.X = 0;
                if (this.bounds.X > v.Width)
                    this.bounds.X = v.Width - this.bounds.Width;

                // move back into the screen - vertical
                if (this.bounds.Y < 0)
                    this.bounds.Y = 0;
                if (this.bounds.Y > v.Height)
                    this.bounds.Y = v.Height - this.bounds.Height;
            }
        }

        public void de_intersect(Vector2 direction, bool reverse_v, bool reverse_h)
        {
            if (!reverse_h)
            {
                if (!reverse_v)
                {
                    bounds.X += (int)direction.X;
                    bounds.Y += (int)direction.Y;
                }
                else // reverse_v == true
                {
                    bounds.X += (int)direction.X;
                    bounds.Y -= (int)direction.Y;
                }
            }
            else// reverse_h == true
            {
                if (!reverse_v)
                {
                    bounds.X -= (int)direction.X;
                    bounds.Y += (int)direction.Y;
                }
                else // reverse_v == true
                {
                    bounds.X -= (int)direction.X;
                    bounds.Y -= (int)direction.Y;
                }
            }
        }
        public void set_container_fade_start(long value)
        {
            fade_in_start_time = value;
        }
        /// <summary>
        /// Rendering function Draw()
        /// </summary>
        /// <param name="engine">Engine object reference</param>
        public void draw(Engine engine, Color color)
        {
            if (customized_background)
                engine.xna_draw(get_background(), get_origin(), null, color * bg_transparency, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // Container background

            //test 
            float val = 1f; // 1f will be used if no fade_in_start time exists

            // draw all elements of this container - allows more control over what is drawn
            // adjust fade-ins
            int counter = 0;
            foreach (UIElementBase element in stored_elements)
            {
                if (fade_in_start_time != 0)
                {
                    val = engine.fade_up(fade_in_start_time + (counter * fade_in_step), fade_in_delay, fade_in_duration, 1.0f);
                }

                element.draw(engine, color, val);

                counter++;
            }
            // draw ghost scrollers if necessary
            if (scrollbar)
            {
                if (fade_in_start_time != 0)
                {
                    val = engine.fade_up(fade_in_start_time, fade_in_delay, fade_in_duration + 200, 1.0f);
                }

                if (current_top_element > minimal_top_element)
                {
                    Texture2D temp = engine.get_texture("ghost_scroller_top");
                    engine.xna_draw(temp, engine.vector_centered(get_rectangle(), temp.Bounds, orientation.horizontal) - new Vector2(0, temp.Height), null, color * (bg_transparency * val), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                }

                if (current_top_element < (this.stored_elements.Count - visible_elements + 1))
                {
                    Texture2D temp = engine.get_texture("ghost_scroller_bottom");
                    engine.xna_draw(temp, engine.vector_centered(get_rectangle(), temp.Bounds, orientation.horizontal) + new Vector2(0, this.bounds.Height), null, color * (bg_transparency * val), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                }
            }
        }
        /// <summary>
        /// Assign  background texture to the container
        /// </summary>
        /// <param name="t">background texture</param>
        /// <returns>true/false success value</returns>
        public bool add_background_texture(Texture2D t)
        {
            try
            {
                background = t;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
        public void add_custom_container_background(Texture2D t)
        {
            // add texture
            background = t;
            customized_background = true;
            // set all element background transparency to 0
            foreach (UIElementBase u in stored_elements)
            {
                u.remove_background();
            }
        }
        /// <summary>
        /// Determine if the Container is currently visible/hidden
        /// </summary>
        /// <returns></returns>
        public bool is_visible()
        {
            return visible;
        }

        public bool scrollbar_enabled()
        {
            return scrollbar;
        }
        /// <summary>
        /// Adds an element to Container. Container bounds recalculated automatically based on number of elements in it
        /// </summary>
        /// <param name="engine">Unit reference</param>
        public void add_element(UIElementBase u)
        {
            u.set_parent(this);

            if (u.get_rectangle().Y == -1)
                u.modify_bounds(u.get_rectangle().X, bounds.Height, u.get_rectangle().Width, u.get_rectangle().Height);

            if (bounds.Width < (u.get_rectangle().Width + u.get_rectangle().X))
                bounds.Width = u.get_rectangle().Width + u.get_rectangle().X;

            bounds.Height = u.get_rectangle().Height + u.get_rectangle().Y;

            stored_elements.Add(u);

            // recalculate a serializeable surrogate for Rectangle type
            bounds_surrogate = Engine.rectangle_to_delimited_string(bounds);
        }

        public UIElementBase find_element(actions a)
        {
            foreach (UIElementBase u in stored_elements)
            {
                if (u.get_action() == a)
                {
                    return u;
                }
            }
            return null;
        }
        /// <summary>
        /// Returns current container name
        /// </summary>
        /// <returns>container name</returns>
        public String get_name()
        {
            return container_name;
        }
        /// <summary>
        /// Access a full list of stored_elements/stored_elements which exist in this container
        /// </summary>
        /// <returns>A List<UIelement> of all stored_elements in this container</returns>
        public List<UIElementBase> get_element_list()
        {
            return stored_elements;
        }
        /// <summary>
        /// Get boundaries of thsi container
        /// </summary>
        /// <returns>Overall Rectangle bounds of container</returns>
        public Rectangle get_rectangle()
        {
            return bounds;
        }
        /// <summary>
        /// Get background color
        /// </summary>
        /// <returns>Color value of the container background</returns>
        public Color get_color()
        {
            return bg_color;
        }
        /// <summary>
        /// Get bakcground transparency value
        /// </summary>
        /// <returns>float 0-1f value of background transparency</returns>
        public float get_transparency()
        {
            return bg_transparency;
        }
        /// <summary>
        /// Access background texture
        /// </summary>
        /// <returns>Texture2d container background</returns>
        public Texture2D get_background()
        {
            return background;
        }
        /// <summary>
        /// Origin vector - draw Container at these coordinates on screen
        /// </summary>
        /// <returns>Vector2 origin value</returns>
        public Vector2 get_origin()
        {
            return new Vector2(bounds.X, bounds.Y);
        }
        /// <summary>
        /// Action assigned to this container
        /// </summary>
        /// <returns>Action enum value or null</returns>
        public actions? get_action()
        {
            return click_action;
        }
        /// <summary>
        /// This function changes container origin to move container and its stored_elements on screen. E.g. a context menu can be displayed at a different place
        /// </summary>
        /// <param name="new_origin">New origin position where Container is drawn</param>
        public void set_origin(Vector2 new_origin)
        {
            bounds.X = (int)new_origin.X;
            bounds.Y = (int)new_origin.Y;
        }
        /// <summary>
        /// Set visibility value for this Container
        /// </summary>
        /// <param name="value">true/false value - visible or hidden</param>
        public void set_visibility(bool value)
        {
            visible = value;
        }
        public void make_visible(Engine engine)
        {
            set_container_fade_start(engine.get_current_game_millisecond());
            visible = true;
        }
        public void set_transparency(float value)
        {
            bg_transparency = value;
        }
    } // class end
}// namespace end
