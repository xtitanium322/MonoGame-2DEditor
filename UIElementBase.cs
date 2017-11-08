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

namespace EditorEngine
{
    /// <summary>
    /// Ensure that a UI element creates its basic building block - content sectors
    /// </summary>
    public interface ISectored
    {
        void create_sectors();
    }
    /// <summary>
    /// Base of each UI element - contains the shared functionality
    /// </summary>
    [Serializable()]
    public class UIElementBase : ISectored
    {
        protected string element_id;                            // each possible GUI element wil have an id for searching
        protected Container parent;                             // reference to the Container hosting this element
        protected List<horizontal_sector> zones;                // contains all horizontal/list of vertical zones that exist in this Unit. By default, each unit must have 1 horizontal zone
        [NonSerialized]
        protected Texture2D icon;               // icon / or a reference to a Texture2D dictionary (build later)
        [NonSerialized]
        protected Texture2D background;         // texture holding Unit'engine background
        [NonSerialized]
        protected Texture2D tooltip;            // background for tooltip
        [NonSerialized]
        protected Rectangle bounds;                             // element dimensions
        [NonSerialized]
        protected Color bg_color;                               // overall background
        protected float bg_transparency;                        // background transparency
        protected String label;                                 // display label 
        protected String tooltip_text;                          // text value for a tooltip to display on hover
        protected bool visible;                                 // hide or show
        protected bool confirmation;                            // action assigned to this Unit needs to be confirmed before it happens, for example - delete all cells in current world
        protected bool active;
        protected actions? _action;                             // what to do on click
        protected state current_state;                          // default / hovered 
        public type function;                                  // button, slider, choice (yes/no)
        private orientation label_positioning;                  // custom positioning of label inside background
        protected long hover_start_time = 0;                    // millisecond when current hover started
        protected const float hover_fade_delay = 200f;          // amount of time needed to start tooltip fade in
        protected const float hover_fade_duration = 500f;       // amount of time transparency value changes from 0 to 1f after fade delay has ended
        protected const float tooltip_termination_time = 3500f; // time since hover start when tooltip is no longer shown
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">element string id for lookup</param>
        /// <param name="parent">parent container object</param>
        /// <param name="f">type of the element</param>
        /// <param name="c">action assigned to the element</param>
        /// <param name="safety">confirm or no</param>
        /// <param name="dimension">element size and position inside the container</param>
        /// <param name="icon">does it have an icon? null if not</param>
        /// <param name="label">string label to write on top of the proper sector</param>
        /// <param name="tooltip">tooltip to show on hover</param>
        public UIElementBase(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
        {
            this.element_id = id;
            this.parent = parent;
            background = null;
            bounds = dimension;
            bg_color = Color.White; // default color placeholder
            bg_transparency = 1f;
            this.label = label;
            this.icon = icon;
            this.visible = true; // all stored_elements should be visible
            _action = c;
            current_state = state.default_state;
            function = f;
            this.tooltip_text = tooltip.ToString();
            zones = new List<horizontal_sector>();
            label_positioning = orientation.both; //default
            //create_sectors(f);
            confirmation = (safety == confirm.yes) ? true : false; // assign confirmation dialog box 
        }
        /// <summary>
        /// Interface function to be created in each element
        /// </summary>
        public virtual void create_sectors()// recreate in derived classes
        { }
        /// <summary>
        /// Draw the masking graphic
        /// </summary>
        /// <param name="e">engine instance</param>
        public virtual void draw_masking_sprite(Engine e)
        {
            // required to apply shader effects on elements, such as a crop effect 
        }
        /// <summary>
        /// Draw the post processing graphcis
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="interface_color">interface color value</param>
        /// <param name="interface_transparency">transparency value 0-1</param>
        public virtual void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            // required to include overlaying graphics after effects have been completed
        }
        /// <summary>
        /// Load a custom background for this element
        /// </summary>
        /// <param name="background">Texture2d object</param>
        public void load_custom_background(Texture2D background)
        {
            this.background = background;
        }
        /// <summary>
        /// Add a parent container
        /// </summary>
        /// <param name="c">container object</param>
        public void set_parent(Container c)
        {
            parent = c;
        }
        /// <summary>
        /// Get parent container value
        /// </summary>
        /// <returns>container object</returns>
        public Container get_parent()
        {
            return parent;
        }
        /// <summary>
        /// Set the label value
        /// </summary>
        /// <param name="label">label string</param>
        public void set_label(String label)
        {
            this.label = label;
        }
        /// <summary>
        /// Assign action to the element
        /// </summary>
        /// <param name="action">action to take on click</param>
        public void set_action(actions action)
        {
            this._action = action;
        }
        /// <summary>
        /// Does action require confirmation
        /// </summary>
        /// <returns>true or false</returns>
        public bool required_confirmation()
        {
            return confirmation;
        }
        /// <summary>
        /// Set current_state - default, hovered or highlighted
        /// </summary>
        /// <param name="c">State enum - default - hovered</param>
        public void set_state(state c)
        {
            current_state = c;
        }
        /// <summary>
        /// Return current current_state of the GUI unit 
        /// </summary>
        /// <returns>Current unit current_state enum - default - hovered</returns>
        public state get_state()
        {
            return current_state;
        }
        /// <summary>
        /// Get the string id of this element
        /// </summary>
        /// <returns>string id</returns>
        public string get_id()
        {
            return element_id;
        }
        /// <summary>
        /// Detects hover on this GUI unit (Unused)
        /// </summary>
        /// <returns> bool value true/false</returns>
        public bool detect_hover()
        {
            return false;
        }
        /// <summary>
        /// Is the element visible?
        /// </summary>
        /// <returns>true or false</returns>
        public bool is_visible()
        {
            return visible;
        }
        /// <summary>
        /// Update element visibility
        /// </summary>
        /// <param name="value">true or false</param>
        public void set_visible(bool value)
        {
            visible = value;
        }
        /// <summary>
        /// Update visibility - toggle current
        /// </summary>
        /// <param name="val">string "toggle"</param>
        public void set_visible(string val)
        {
            if(val.Equals("toggle"))
            {
                visible = !visible; // toggle value
            }
        }
        /// <summary>
        /// Rendering function for the entire GUI unit. Draw()
        /// </summary>
        /// <param name="engine">Engine object</param>
        public virtual void draw(Engine engine, Color color, float transparency)
        {
            // skip drawing this Unit if it is invisible
            if (!visible)
                return;
            // background or label
            if (current_state == state.default_state) // not hovered
            {
                if (background != null)                                            // multiplying current transparency by  percentage passed from UI based on container fade
                    engine.xna_draw(get_background(), get_origin(), null, color * (bg_transparency * transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
            }
            else // hovered
            {// if this element is hovered - original design: change transparency values using sine wave fading algorithm, new design: simply make color 25% darker using adjusted_color function of game engine class
                if (background != null)
                {
                    if (bg_transparency == 1)
                        //engine.xna_draw(get_background(), get_origin(), null, color * engine.fade_sine_wave_smooth(200, 0.85f, 1f), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                        engine.xna_draw(get_background(), get_origin(), null, engine.adjusted_color(color, 0.75f), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                    else
                        engine.xna_draw(get_background(), get_origin(), null, engine.adjusted_color(color, 0.75f), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                }
            }
            // based on zones created - draw other components here
            if (zones.Count > 0)
            {
                // draw
                foreach (horizontal_sector h in zones)
                {
                    foreach (vertical_sector v in h.get_verticals())
                    {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                        // calculate curent vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                        Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());
                        // draw an element in calculated position
                        switch (v.get_content_type())
                        {
                            case sector_content.active_indicator:
                                {
                                    if (active)
                                    {
                                        Texture2D indicator = engine.get_texture("icon-active-indicator");
                                        Vector2 draw_origin = engine.vector_centered(draw_zone, indicator.Bounds, orientation.both);
                                        engine.xna_draw(indicator, draw_origin, null, Color.White * Engine.fade_sine_wave_smooth(2000, 0.25f, 1f, sinewave.zero), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                    }
                                }
                                break;
                            case sector_content.icon:
                                {
                                    Texture2D icon = get_icon();
                                    if (icon != null)
                                    {
                                        Vector2 draw_origin = engine.vector_centered(draw_zone, icon.Bounds, orientation.both);
                                        engine.xna_draw(icon, draw_origin, null, Color.White * (bg_transparency * transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                    }
                                }
                                break;
                            case sector_content.label:
                                {
                                    if (get_label() != null && get_label() != "")
                                    {
                                        Vector2 dimensions = engine.get_UI_font().MeasureString(get_label());
                                        Vector2 draw_origin = engine.vector_centered(draw_zone, dimensions, label_positioning); // vector_centered must return truncated pixel values to eliminate blurred text 
                                        engine.xna_draw_outlined_text(get_label(), draw_origin, Vector2.Zero, Color.White * transparency, Color.Black, engine.get_UI_font());
                                    }
                                }
                                break;
                            default:
                                break;
                        }//end switch
                    }// end foreach vertical
                }// end foreach horizontal
            }// end if
        }
        /// <summary>
        /// Darw the tooltip
        /// </summary>
        /// <param name="engine">engine instance</param>
        public void draw_tooltip(Engine engine)
        {
            // draw a tooltip if hovered, the element is visible and the tooltip text exists
            if (this.current_state == state.hovered && this.visible==true && tooltip_text != null && tooltip_text != "")
            {
                Vector2 position = engine.get_mouse_vector() + new Vector2(25, 0); // display position adjusted for pointer texture
                Vector2 text_size = engine.get_UI_font().MeasureString(tooltip_text.ToString());
                Vector2 centered_text_position = engine.vector_centered(new Rectangle((int)position.X, (int)position.Y, (int)tooltip.Width, (int)tooltip.Height), new Rectangle(0, 0, (int)text_size.X, (int)text_size.Y), orientation.both);

                // calculate transparency based on delay and fade time period
                float transparency_internal = Engine.fade_up((float)hover_start_time, hover_fade_delay, hover_fade_duration);

                // draw tooltip with text using a fade in effect based on hover start time and delay
                if (Engine.get_current_game_millisecond() - hover_fade_delay >= hover_start_time            // existed long enough to be faded in
                    && Engine.get_current_game_millisecond() - hover_start_time < tooltip_termination_time) // within existence timeframe
                {
                    engine.xna_draw(tooltip, position, null, Color.White * transparency_internal, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // background
                    engine.xna_draw_outlined_text(tooltip_text.ToString(), centered_text_position, Vector2.Zero, Color.AntiqueWhite * transparency_internal, Color.Black * transparency_internal, engine.get_UI_font());
                }
            }
        }

        /// <summary>
        /// Updates current GUI unit label for this unit. E.g. Label is displayed on top of the button
        /// </summary>
        /// <param name="new_label">New value for the label</param>
        public void update_label(String new_label)
        {
            label = new_label;
        }
        /// <summary>
        /// Assign a texture for the overall unit background
        /// </summary>
        /// <param name="t">Object reference for the background texture being assigned</param>
        /// <returns>bool values of success true/false</returns>
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

        /// <summary>
        /// Create a tooltip background for the element if there is a tooltip
        /// </summary>
        /// <param name="t">Texture assigned to a tooltip background image</param>
        /// <returns>true if tooltip was added</returns>
        public bool add_tooltip_background(Texture2D t)
        {
            try
            {
                tooltip = t;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
        /// <summary>
        /// Update transparency value
        /// </summary>
        /// <param name="value">0-1 float</param>
        public void set_transparency(float value)
        {
            bg_transparency = value;
        }
        /// <summary>
        /// Access tooltip text
        /// </summary>
        /// <returns>tooltip text String value</returns>
        public String get_tooltip_text()
        {
            if (tooltip_text != null)
                return tooltip_text.ToString();
            else
                return "";
        }
        /// <summary>
        /// Sets millisecond marker for current hover start moment.
        /// </summary>
        /// <param name="millisecond">Current in-game millisecond</param>
        public void set_hover_start_time(long millisecond)
        {
            hover_start_time = millisecond;
        }

        /// <summary>
        /// Activates this button/element
        /// </summary>
        public void activate()
        {
            active = true;
        }
        /// <summary>
        /// Deactivates this button/element
        /// </summary>
        public void inactivate()
        {
            active = false;
        }

        /// <summary>
        /// Access current icon texture for this Unit if it exists
        /// </summary>
        /// <returns>Either texture2d or a null - icon assigned to this unit</returns>
        public Texture2D get_icon()
        {
            return icon;
        }
        /// <summary>
        /// Add an icon texture to the element
        /// </summary>
        /// <param name="icon">icon texture asset</param>
        public void set_icon(Texture2D icon)
        {
            this.icon = icon;
        }
        /// <summary>
        /// Access overall unit rectangle bounds
        /// </summary>
        /// <returns>Rectangle - unit bounds</returns>
        public Rectangle get_rectangle()
        {
            return bounds;
        }
        /// <summary>
        /// Access unit background color 
        /// </summary>
        /// <returns>Color value of the background</returns>
        public Color get_color()
        {
            return bg_color;
        }
        /// <summary>
        /// Return transparency value for this unit'engine background
        /// </summary>
        /// <returns>float 0-1f transparency value</returns>
        public float get_transparency()
        {
            return bg_transparency;
        }
        /// <summary>
        /// Access background texture for this unit
        /// </summary>
        /// <returns>Texture2d of the overall unit background</returns>
        public Texture2D get_background()
        {
            return background;
        }
        /// <summary>
        /// Access top left corner - origin at which this unit should be drawn inside it'engine container. This value should be added to parent origin of the Container.
        /// </summary>
        /// <returns>Vector of the unit origin inside container. Usually 0,n </returns>
        public Vector2 get_origin()
        {
            return new Vector2(bounds.X + parent.get_origin().X, bounds.Y + parent.get_origin().Y);
        }
        /// <summary>
        /// Get unit'engine displayable label
        /// </summary>
        /// <returns>String value of the label</returns>
        public String get_label()
        {
            return label;
        }
        /// <summary>
        /// Get action assigned to this unit
        /// </summary>
        /// <returns> action enum of the unit</returns>
        public actions? get_action()
        {
            return _action;
        }
        /// <summary>
        /// Get unit'engine contexttype
        /// </summary>
        /// <returns>contexttype enum value - slider,choice,button etc.</returns>
        public type get_type()
        {
            return function;
        }
        /// <summary>
        /// Remove the background from this element
        /// </summary>
        public void remove_background()
        {
            background = null;
        }
        /// <summary>
        /// Position the label in a different orientation
        /// </summary>
        /// <param name="value"></param>
        public void set_custom_label_positioning(orientation value)
        {
            label_positioning = value;
        }
        /// <summary>
        /// Update element size and position
        /// </summary>
        /// <param name="x">x coordinate inside container</param>
        /// <param name="y">y coordinate inside container</param>
        /// <param name="width">element width in pixels</param>
        /// <param name="height">element height in pixels</param>
        public void modify_bounds(int x, int y, int width, int height)
        {
            bounds.X = x;
            bounds.Y = y;
            bounds.Width = width;
            bounds.Height = height;
        }
    }
}
