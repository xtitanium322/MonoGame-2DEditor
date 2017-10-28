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
/*
 ALL elements available for the user interface. Custom built.
 */
namespace EditorEngine
{
    //-----------------------------------------------------------------------------------------------------------------------
    [Serializable()]
    class Slider : UIElementBase
    {
        [NonSerialized]
        private Rectangle slider_bounds;
        [NonSerialized]
        private Texture2D slider_background;        //2 background of the slider area / can also be a slider line - to display center in the background area (slider_bounds)
        [NonSerialized]
        private Texture2D slider;                   //2 texture of a slider itself - not background
        public float slider_value;                  //2 numeric value associated with this slider. slider itself is represented by 0-100% 
        public float min_slider_value;              //2 minimal value
        public float max_slider_value;              //2 maximum value of the slider
        private int slider_precision;               //2 0 = show as int, 1 or 2 = show as float in string labels

        public Slider(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            slider_value = 0.0f;
            slider_precision = 0;
            create_sectors();
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public new void create_sectors()
        {
            horizontal_sector temp1 = new horizontal_sector(0, (int)bounds.Height / 2);
            temp1.add_vertical(new vertical_sector(0, (int)bounds.Width / 2, sector_content.label));
            temp1.add_vertical(new vertical_sector((int)bounds.Width / 2, bounds.Width, sector_content.current_slider_value));
            horizontal_sector temp2 = new horizontal_sector((int)bounds.Height / 2, (int)bounds.Height);
            temp2.add_vertical(new vertical_sector(0, 30, sector_content.min_slider));
            temp2.add_vertical(new vertical_sector(((int)bounds.Width / 2 - 75), ((int)bounds.Width / 2 + 75), sector_content.slider_area)); // slider area is always 150px 
            slider_bounds = new Rectangle(((int)(bounds.Width / 2) - 75), temp2.get_ys() /*+ (int)this.get_origin().Y*/, 150, temp2.get_height()); // create slider bounds based on sector position inside this Unit ^. add Y value of the origin in case this is not the first unit in container
            temp2.add_vertical(new vertical_sector(bounds.Width - 30, bounds.Width, sector_content.max_slider));
            zones.Add(temp1);
            zones.Add(temp2);
        }

        public void set_slider_precision(int value)
        {
            if (value < 0 || value > 3)
                return;
            else slider_precision = value;
        }

        /// <summary>Access slider area background texture</summary>
        /// <returns>Texture of a slider background area</returns>
        public Texture2D get_slider_background()
        {
            return slider_background;
        }
        /// <summary>Sets slider values</summary>
        /// <param name="current_value">What the value is currently (for this slider)</param>
        /// <param name="max_value">Maximum value allowed for associated value</param>
        public void set_slider_values(float current_value, float min_value, float max_value, int precision = 0)
        {
            slider_precision = precision; // for label display only

            if (function == type.slider)
            {
                slider_value = current_value;
                min_slider_value = min_value;
                max_slider_value = max_value;
            }
        }

        /// <summary>
        /// Get slider area information
        /// </summary>
        /// <returns>Slider area bounds rectangle</returns>
        public Rectangle get_slider_bounds()
        {
            return slider_bounds;
        }
        /// <summary>
        /// Access slider value
        /// </summary>
        /// <returns>Current value associated with the slider</returns>
        public float get_slider_value()
        {
            return slider_value;
        }
        public void set_slider_value(float value)
        {
            slider_value = value;
        }

        public int get_slider_value_int()
        {
            return (int)Math.Floor(slider_value);
        }
        /// <summary>
        /// randomize slider value
        /// </summary>
        /// <param name="engine">Engine helper contains "random" function</param>
        public void set_random_slider_value(Engine engine)
        {
            if (max_slider_value > 1f)
                slider_value = engine.generate_float_range(min_slider_value, max_slider_value);
            else // set transparency value at 1 to show the best color 
                slider_value = max_slider_value;
        }

        /// <summary>
        /// Updates slider component of an element
        /// </summary>
        /// <param name="mouse_position"> Vector describing current mouse position x and y</param>
        public void update(Vector2 mouse_position)
        {
            Rectangle adjusted_bounds = slider_bounds;
            adjusted_bounds.X += parent.get_rectangle().X;
            adjusted_bounds.Y += parent.get_rectangle().Y;
            // set slider value based on mouse input and min/max values
            float percentage = get_slider_percentage(mouse_position);
            slider_value = (percentage * (max_slider_value - min_slider_value)) + min_slider_value;
        }
        /// <summary>
        /// Calculates new slider position based on the mouse input      
        /// </summary>
        /// <param name="mouse_position">Vector describing current mouse position x and y</param>
        /// <returns> New percentage value of this slider</returns>
        public float get_slider_percentage(Vector2 mouse_position)
        {
            float percentage;

            if (parent.get_rectangle().X + slider_bounds.X + slider_bounds.Width <= mouse_position.X)
                percentage = 1f;
            else if (parent.get_rectangle().X + slider_bounds.X > mouse_position.X)
                percentage = 0f;
            else
                percentage = (mouse_position.X - (parent.get_rectangle().X + slider_bounds.X)) / slider_bounds.Width; // slider bounds needs to be adjusted to include container position

            return percentage;
        }
        /// <summary>
        /// Unlike same function that calculates percentage based on mouse position for update
        /// This function calculates it as it currently is
        /// </summary>
        /// <returns>percentage value</returns>
        public float get_current_slider_percentage()
        {
            return (slider_value - min_slider_value) / (max_slider_value - min_slider_value);
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            // calculate precision for sliders
            String display_format = "0";
            if (slider_precision == 1)
                display_format = "0.0";
            else if (slider_precision == 2)
            {
                display_format = "0.00";
            }
            else if (slider_precision == 3)
            {
                display_format = "0.000";
            }
            base.draw(engine, color, transparency);
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

                        case sector_content.slider_area: // includes slider line and slider itself
                            {
                                Vector2 draw_origin = engine.vector_centered(draw_zone, slider_background.Bounds, orientation.both);
                                Vector2 slider_progress_origin = engine.vector_centered(draw_zone, engine.get_texture("slider_progress").Bounds, orientation.both);
                                // draw slider line
                                engine.xna_draw(get_slider_background(), draw_origin, null, engine.negative_color(color) * (transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                // draw slider progress texture cropped to fit % of the maximum value
                                engine.xna_draw(engine.get_texture("slider_progress"), slider_progress_origin, new Rectangle(0, 0, (int)(engine.get_texture("slider_progress").Width * get_current_slider_percentage()), 16), engine.negative_color(color) * (transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                // draw slider
                                engine.xna_draw(get_slider(), draw_origin + new Vector2(slider_offset(), -slider.Height / 2 + slider_background.Height / 2), null, engine.negative_color(color) * (transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                            }
                            break;
                        case sector_content.min_slider:
                            {
                                Vector2 min_sl_text = engine.get_UI_font().MeasureString(min_slider_value.ToString(display_format));
                                Vector2 draw_origin = engine.vector_centered(draw_zone, min_sl_text, orientation.both); // vector_centered must return truncated pixel values to eliminate blurred text 
                                engine.xna_draw_text(min_slider_value.ToString(display_format), draw_origin, Vector2.Zero, engine.negative_color(color) * (transparency), engine.get_UI_font());
                            }
                            break;
                        case sector_content.current_slider_value:
                            {
                                Vector2 cur_sl_text = engine.get_UI_font().MeasureString(((int)slider_value).ToString(display_format));
                                Vector2 draw_origin = engine.vector_centered(draw_zone, cur_sl_text, orientation.both); // vector_centered must return truncated pixel values to eliminate blurred text
                                // must make sure that int values are displayed properly
                                if (slider_precision == 0)
                                    engine.xna_draw_text(((int)slider_value).ToString(display_format), draw_origin, Vector2.Zero, engine.negative_color(color) * (transparency), engine.get_UI_font());
                                else
                                    engine.xna_draw_text(slider_value.ToString(display_format), draw_origin, Vector2.Zero, engine.negative_color(color) * (transparency), engine.get_UI_font());

                            }
                            break;
                        case sector_content.max_slider:
                            {
                                Vector2 max_sl_text = engine.get_UI_font().MeasureString(max_slider_value.ToString(display_format));
                                Vector2 draw_origin = engine.vector_centered(draw_zone, max_sl_text, orientation.both); // vector_centered must return truncated pixel values to eliminate blurred text 
                                engine.xna_draw_text(max_slider_value.ToString(display_format), draw_origin, Vector2.Zero, engine.negative_color(color) * (transparency), engine.get_UI_font());
                            }
                            break;
                        default:
                            break;
                    }//end switch
                }// end foreach vertical
            }// end foreach horizontal         
        }// end function

        /// <summary>
        /// Assign a texture for slider background area
        /// </summary>
        /// <param name="t">Object reference for background area texture</param>
        /// <returns>bool values of success true/false</returns>
        public bool add_slider_background(Texture2D t)
        {
            try
            {
                slider_background = t;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
        /// <summary>
        /// Based on value, max value and slider background width - add an engine_offset to slider for display on the background 
        /// </summary>
        /// <returns> Offset in pixels - move slider a pixels to the right in its area</returns>
        public int slider_offset()
        {
            int a = (int)(((slider_value - min_slider_value) / (max_slider_value - min_slider_value)) * (get_slider_bounds().Width - slider.Width)); // subtract slider width to place slider perfectly on the slider-line
            return a;
        }
        /// <summary>
        /// Assign a texture for slider itself
        /// </summary>
        /// <param name="t">Slider texture</param>
        /// <returns>bool value of success true/false </returns>
        public bool add_slider(Texture2D t)
        {
            try
            {
                slider = t;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
        /// <summary>
        /// Access slider texture
        /// </summary>
        /// <returns>Slider texture</returns>
        public Texture2D get_slider()
        {
            return slider;
        }
        /// <summary>
        /// Access origin position of the slider background inside the Container
        /// </summary>
        /// <returns>Vector of the slider background origin inside container</returns>
        public Vector2 get_slider_background_origin()
        {
            return new Vector2(slider_bounds.X, slider_bounds.Y);
        }
    }
    //----------------------------------------------------------------------------------------
    [Serializable()]
    class InfoLabel : UIElementBase
    {

        public InfoLabel(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            create_sectors(); // MUST be in constructor of every derived class
        }

        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height); // create default
            temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.label));
            zones.Add(temp_cb);
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            base.draw(engine, color, transparency);
        }
    }
    //========================================================================================   
    [Serializable()]
    class IDButton<T> : UIElementBase
    {
        protected T tracked_id; // hidden parameter:  whenever action of this element is called outcome will depend on the id stored

        public IDButton(string id, Container parent, type f, actions? c, Rectangle dimension, String label, String tooltip)
            : base(id, parent, f, c, confirm.no, dimension, null, label, tooltip)
        {
            create_sectors(); // MUST be in constructor of every derived class
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default
            temp_cb.add_vertical(new vertical_sector(0, 30, sector_content.icon));
            temp_cb.add_vertical(new vertical_sector(30, bounds.Width, sector_content.label));
            zones.Add(temp_cb);
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            base.draw(engine, color, transparency);
        }
        /// <summary>
        /// Changes tracked value
        /// </summary>
        /// <param name="value">new value</param>
        /// <param name="icon">change button icon if needed</param>
        public void enable_button(T value, Texture2D icon)
        {
            tracked_id = value;
            base.icon = icon;
        }
        public T get_tracked_value()
        {
            return tracked_id;
        }
    }
    // this class creates a tracked button that acts like a lock/unlock. changes color and other features on true/false
    // a specialized variant of switch button and IDButton
    //========================================================================================
    [Serializable()]
    class UIlocker : UIElementBase
    {
        protected bool tracked_id; // true = unlocked, false = locked
        [NonSerialized]
        Color COLOR_LOCKED = Color.OrangeRed;
        [NonSerialized]
        Color COLOR_UNLOCKED = Color.LawnGreen;
        [NonSerialized]
        Texture2D icon_locked;
        [NonSerialized]
        Texture2D icon_unlocked;

        public UIlocker(string id, Container parent, type f, actions? c, Rectangle dimension, String label, String tooltip)
            : base(id, parent, f, c, confirm.no, dimension, null, label, tooltip)
        {
            create_sectors(); // MUST be in constructor of every derived class
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default
            temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.locker_state)); // locker-state = specialized area that notifies user of locker state and changes color
            zones.Add(temp_cb);
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            // call base background drawing function with a different color option
            if (tracked_id)
                base.draw(engine, COLOR_UNLOCKED, transparency);
            else
                base.draw(engine, COLOR_LOCKED, transparency);
            // specific drawing
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate curent vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type())
                    {
                        case sector_content.locker_state:
                            {
                                // draw different lock icons depending on the state
                                if (tracked_id && icon_unlocked != null)
                                {
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, icon_unlocked.Bounds, orientation.both);
                                    engine.xna_draw(icon_unlocked, draw_origin, null, Color.White * (bg_transparency * transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                }
                                else if (!tracked_id && icon_locked != null)
                                {
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, icon_locked.Bounds, orientation.both);
                                    engine.xna_draw(icon_locked, draw_origin, null, Color.White * (bg_transparency * transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Changes tracked value
        /// </summary>
        /// <param name="value">new value</param>
        /// <param name="icon">change button icon if needed</param>
        public void enable_button(bool value, Texture2D icon_locked, Texture2D icon_unlocked)
        {
            tracked_id = value;
            this.icon_locked = icon_locked;
            this.icon_unlocked = icon_unlocked;
        }
        public bool get_tracked_value()
        {
            return tracked_id;
        }
        public void toggle_lock()
        {
            tracked_id = !tracked_id;
        }
    }
    //========================================================================================
    [Serializable()]
    class ColorPreviewButton : UIElementBase
    {
        [NonSerialized]
        private Color preview;

        public ColorPreviewButton(string id, Container parent, type f, actions? c, Rectangle dimension, String label, String tooltip)
            : base(id, parent, f, c, confirm.no, dimension, null, label, tooltip)
        {
            create_sectors(); // MUST be in constructor of every derived class
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(5, bounds.Height - 5);     // create default
            temp_cb.add_vertical(new vertical_sector(5, bounds.Width - 5, sector_content.color_preview));
            zones.Add(temp_cb);
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            base.draw(engine, color, transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate curent vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());
                    // draw an element in calculated position
                    switch (v.get_content_type())
                    {
                        case sector_content.color_preview:
                            {
                                engine.xna_draw(Game1.pixel_texture, new Vector2(draw_zone.X, draw_zone.Y), draw_zone, preview * (transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                Vector2 font_position = engine.vector_centered(draw_zone, engine.get_UI_font().MeasureString("color preview"), orientation.both);
                                engine.xna_draw_text("color preview", font_position, Vector2.Zero, engine.inverted_color(preview) * transparency, engine.get_UI_font());
                                engine.xna_draw_rectangle_outline(draw_zone, Color.Black * transparency, 1);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void update(Color preview_value)
        {
            preview = preview_value;
        }
    }
    //---------------------------------------------------------------------------------------------------------
    [Serializable()]
    public class Button : UIElementBase
    {
        private Container sub_context; // assign expandable container to a expandable button, regular button will not use this

        public Button(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            sub_context = null;
            create_sectors(); // MUST be in constructor of every derived class
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        public new void create_sectors()
        {
            if (function == type.button)
            {
                horizontal_sector temp = new horizontal_sector(0, bounds.Height); // create default
                temp.add_vertical(new vertical_sector(0, 30, sector_content.icon));
                temp.add_vertical(new vertical_sector(30, this.bounds.Width - 30, sector_content.label)); // leave room for indicator
                temp.add_vertical(new vertical_sector(this.bounds.Width - 30, this.bounds.Width, sector_content.active_indicator));
                zones.Add(temp);
            }
            else if (function == type.expandable_button)
            {
                horizontal_sector temp_ex = new horizontal_sector(0, bounds.Height); // create default
                temp_ex.add_vertical(new vertical_sector(0, 30, sector_content.icon));
                temp_ex.add_vertical(new vertical_sector(30, this.bounds.Width - 30, sector_content.label)); // leave room for indicator
                temp_ex.add_vertical(new vertical_sector(this.bounds.Width - 30, this.bounds.Width, sector_content.expansion_indicator));
                zones.Add(temp_ex);
            }
        }

        /// <summary>
        /// Assign a Container which holds all sub-context elements to this expansion-button
        /// </summary>
        /// <param name="c">Container reference</param>
        public void assign_sub_context(Container c)
        {
            sub_context = c;
        }
        public Container get_context()
        {
            return sub_context;
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            if (!visible)
                return;
            // do basic rendering
            base.draw(engine, color, transparency);

            if (zones.Count > 0)
            {
                foreach (horizontal_sector h in zones)
                {
                    foreach (vertical_sector v in h.get_verticals())
                    {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                        // calculate curent vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                        Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());
                        // draw an element in calculated position
                        switch (v.get_content_type())
                        {
                            case sector_content.expansion_indicator:
                                {
                                    if (sub_context != null)
                                    {
                                        Texture2D indicator = engine.get_texture("icon-expansion-indicator");
                                        Vector2 draw_origin = engine.vector_centered(draw_zone, indicator.Bounds, orientation.both);
                                        engine.xna_draw(indicator, draw_origin, null, engine.negative_color(color) * (transparency), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
    //----------------------------------------------------------------------------------------------
    /// <summary>
    /// Generic value tracking GUI element. 
    /// For example, bool = tracking binary true/false scenarios such as on/off, enumtype = tracking a series of defined enums
    /// </summary>
    /// <typeparam name="T">contexttype of value tracked by this GUI element</typeparam>
    [Serializable()]
    class SwitchButton<T> : UIElementBase
    {
        private T value;
        //private List<T> permitted_values; // use this for ints and floats
        // base. action = option_value_switch;

        public SwitchButton(string id, T value, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip) // action.option_value_switch
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            this.value = value;
            create_sectors(); // MUST be in constructor of every derived class
        }
        /// <summary>
        /// Create internal sectors inside this element
        /// </summary>
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height); // create default
            temp_cb.add_vertical(new vertical_sector(0, (bounds.Width - 60), sector_content.label)); // element label
            temp_cb.add_vertical(new vertical_sector((bounds.Width - 60), bounds.Width, sector_content.yn_option));
            zones.Add(temp_cb);
        }
        public new void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);
        }
        public new void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);
        }
        /// <summary>
        /// Draw this element
        /// </summary>
        /// <param name="engine">Engine</param>
        /// <param name="color">Element color defined by GUI</param>
        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            base.draw(engine, color, transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate curent vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type())
                    {
                        case sector_content.yn_option:
                            {
                                // transform bool to text representation
                                String display_value = string.Format("{0:yes;0;no}", value.GetHashCode());
                                Vector2 dimensions = engine.get_UI_font().MeasureString(display_value);
                                Vector2 draw_origin = engine.vector_centered(draw_zone, dimensions, orientation.both);
                                engine.xna_draw_text(display_value, draw_origin, Vector2.Zero, Color.DarkSlateGray * (transparency), engine.get_UI_font());
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Get tracked value current state
        /// </summary>
        /// <returns></returns>
        public T get_tracked_value()
        {
            return value;
        }
        /// <summary>
        /// Sets initial value for this element
        /// </summary>
        /// <param name="value"></param>
        public void set_value(T value)
        {
            this.value = value;
        }
    }
    //------------------------------------------------------------------------------------------------------------------  
    [Serializable()]
    class ProgressBar : UIElementBase // tooltip will let user know what is being tracked
    {
        [NonSerialized]
        private Color progress_color; // progress bar will be made this color
        int max_value; // necessary for progress tracking variant of value tracker element
        int min_value;
        int tracked_id; //shows current value
        private bool percentage_mode; // number shown in the progress sector will be a percentage value, if false - it will be tracked number itself
        private bool show_value; // will show tracked value in percentage or raw form. if false - hides value
        [NonSerialized]
        private Texture2D progress_bar_sprite; // create a rectangular texture with size equal to sector bounds, draw cropped horizontally to show percentage of the max value 
        [NonSerialized]
        private Texture2D graphics_mask; // a custom sprite that will give progress bar a custom look (e.g. curved)
        [NonSerialized]
        private Texture2D border_sprite; // will put an always visible border in the shape of graphics mask on the graphic element

        public ProgressBar(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip, Color progress_color, bool percentage_mode, bool show_value)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            this.progress_color = progress_color;
            this.percentage_mode = percentage_mode;
            this.show_value = show_value;
            tracked_id = 0;
            min_value = 0;
            max_value = 0;
            create_sectors();
            set_progress_sprite();
        }
        public override void draw_masking_sprite(Engine e)
        {
            base.draw_masking_sprite(e);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.progress: // sector unique to this element
                            {
                                Vector2 draw_origin = e.vector_centered(draw_zone, progress_bar_sprite.Bounds, orientation.vertical);
                                // drawing progress bar mask (needs to be separated into a process that applies shader effect - draw to two separate surfaces and then erase all unmasked areas)
                                e.xna_draw(graphics_mask, draw_origin, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        public override void draw_post_processing(Engine e, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(e, interface_color, interface_transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.progress: // sector unique to this element
                            {
                                Vector2 draw_origin = e.vector_centered(draw_zone, progress_bar_sprite.Bounds, orientation.vertical);
                                // drawing progress bar mask (needs to be separated into a process that applies shader effect - draw to two separate surfaces and then erase all unmasked areas)
                                e.xna_draw(border_sprite, draw_origin, null, e.adjusted_color(interface_color, 0.8f), 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Create sectors for this user interface element.
        /// -40 in the width parameters is a zone for numeric value
        /// </summary>
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default
            temp_cb.add_vertical(new vertical_sector(bounds.Width - 40, bounds.Width, sector_content.icon));
            temp_cb.add_vertical(new vertical_sector(0, bounds.Width - 40, sector_content.progress)); // new value for sector content - fill the entire sector with an adjustable colored overlay (also show the percentage value filled, or the actual number)
            //temp_cb.add_vertical(new vertical_sector(0, bounds.Width-40, sector_content.progress_label));
            zones.Add(temp_cb);
        }
        public void set_progress_sprite()
        {
            // one-time texture generation based on sector size 
            // add customization options later if needed, e.g. buffer empty zone above and below texture , so it doesn't fill the entire region.
            progress_bar_sprite = Game1.create_colored_rectangle(get_rectangle(), progress_color, 1f);
        }
        public void set_progress_color(Color c)
        {
            progress_color = c;
        }
        public void set_mask(Engine e)
        {
            // one-time texture generation based on sector size 
            // add customization options later if needed, e.g. buffer empty zone above and below texture , so it doesn't fill the entire region.
            graphics_mask = e.get_texture("progress_mask");
        }
        public void set_border(Engine e)
        {
            // one-time texture generation based on sector size 
            // add customization options later if needed, e.g. buffer empty zone above and below texture , so it doesn't fill the entire region.
            border_sprite = e.get_texture("progress_border");
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            //base.draw(engine, color, transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.progress: // sector unique to this element
                            {
                                // define drawing sequence here
                                // background and progress bar
                                int min = Convert.ToInt32(min_value);
                                int max = Convert.ToInt32(max_value);
                                int curr = Convert.ToInt32(tracked_id);
                                Vector2 draw_origin = engine.vector_centered(draw_zone, progress_bar_sprite.Bounds, orientation.vertical);
                                //drawing progress bar rectangular shape
                                engine.xna_draw(progress_bar_sprite, draw_origin, new Rectangle((int)draw_origin.X, (int)draw_origin.Y, (int)(v.get_width() * engine.get_percentage_of_range(min, max, curr)), h.get_height()), progress_color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
                            }
                            break;
                        case sector_content.icon:
                            {
                                if (show_value)
                                {
                                    int curr = Convert.ToInt32(tracked_id);
                                    Vector2 cur_sl_text = engine.get_UI_font().MeasureString(curr.ToString());
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, cur_sl_text, orientation.both);
                                    engine.xna_draw_outlined_text(curr.ToString(), draw_origin, Vector2.Zero, Color.Black, Color.White, engine.get_UI_font());
                                }
                            }
                            break;
                        case sector_content.progress_label:
                            {
                                if (show_value)
                                {
                                    Vector2 cur_sl_text = engine.get_UI_font().MeasureString(get_label());
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, cur_sl_text, orientation.both);
                                    engine.xna_draw_outlined_text(get_label(), draw_origin, Vector2.Zero, Color.White, Color.BlueViolet, engine.get_UI_font());
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        // element specific functions below
        public void set_element_values(int min_value, int max_value, int current = 0)
        {
            this.min_value = min_value;
            this.max_value = max_value;
            this.tracked_id = current;
        }
        public void update(int value)
        {
            this.tracked_id = value;
        }

    }
    //------------------------------------------------------------------------------------------------------------------
    [Serializable()]
    class ProgressCircle : UIElementBase // tooltip will let user know what is being tracked
    {
        [NonSerialized]
        private Color progress_color; // progress circle will be made this color
        [NonSerialized]
        private Color backing_plate_color = Color.Black;
        int max_value; // necessary for progress tracking variant of value tracker element
        int min_value;
        int tracked_id; //shows current value
        private bool percentage_mode; // number shown in the progress sector will be a percentage value, if false - it will be tracked number itself
        private bool show_value; // will show tracked value in percentage or raw form. if false - hides value
        [NonSerialized]
        private Texture2D half_circle; // 2 halves will be positioned and rotated according to percentage needed
        [NonSerialized]
        private Texture2D background_circle; // used to create background for half-circles
        [NonSerialized]
        private Texture2D mask_blocker; // another half circle used to block left side, when partial (<50%) half circle is rotated - will be used as a mask IF % is below 50.
        [NonSerialized]
        private Texture2D mask_mid_circle; // center piece of the circle
        private const int graphic_quality_precision = 1024; // this is a size of circle created, larger value makes edges less jagged (2048 IS MAX TEXTURE SIZE in XNA, MonoGame supports 4096)

        public ProgressCircle(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip, Color progress_color, bool percentage_mode, bool show_value)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            this.progress_color = progress_color;
            this.percentage_mode = percentage_mode;
            this.show_value = show_value;
            tracked_id = 0;
            min_value = 0;
            max_value = 0;
            create_sectors();
            set_half_circle_sprite();
            set_mask();
        }

        public void set_progress_color(Color progress_color)
        {
            this.progress_color = progress_color;
        }

        public void set_mask()
        {
            // one-time texture generation based on sector size 
            // add customization options later if needed, e.g. buffer empty zone above and below texture , so it doesn't fill the entire region.
            mask_mid_circle = Game1.createCircle(graphic_quality_precision, Color.Black, 20f); // smaller inner circle
            mask_blocker = Game1.createHalfCircle(bounds.Width + 4, Color.Black, 1f); // blocks left half of circle
        }

        public void set_half_circle_sprite()
        {
            // one-time texture generation based on sector size 
            // add customization options later if needed, e.g. buffer empty zone above and below texture , so it doesn't fill the entire region.
            half_circle = Game1.createHalfCircle(graphic_quality_precision, Color.White, 30f);

            // also create  full circular backing plate (draw in a slightly exaggerated scale)
            background_circle = Game1.createCircle(graphic_quality_precision, Color.White, 10f);
        }
        public override void draw_masking_sprite(Engine engine)
        {
            base.draw_masking_sprite(engine);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.circular_progress:
                            {
                                // define drawing sequence here
                                // background and progress bar
                                int min = Convert.ToInt32(min_value);
                                int max = Convert.ToInt32(max_value);
                                int curr = Convert.ToInt32(tracked_id);
                                float percent = engine.get_percentage_of_range(min, max, curr);
                                percent = percent > 1f ? 1f : percent; // clip to 100% (if larger)
                                percent = percent < 0f ? 0f : percent; // clip to 0% (if smaller)

                                Vector2 draw_origin = engine.vector_centered(draw_zone, mask_blocker.Bounds, orientation.both);
                                //drawing mid circle mask
                                draw_origin = engine.vector_centered(draw_zone, mask_mid_circle.Bounds, orientation.both);
                                Vector2 draw_adjustment = new Vector2(mask_mid_circle.Width / 2, mask_mid_circle.Height / 2); // required due to scaling
                                engine.xna_draw(mask_mid_circle, draw_origin + draw_adjustment, null, Color.White, 0f, new Vector2(mask_mid_circle.Width / 2, mask_mid_circle.Height / 2), ((float)(bounds.Width - 25) / (float)graphic_quality_precision), SpriteEffects.None, 0.9f); // mid circle blocker

                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Reserved for borders and/or text elements that fall into potential mask territory (will be erased if drawn before masking effect runs)
        /// </summary>
        /// <param name="engine">engine object</param>
        /// <param name="interface_color">interface color</param>
        /// <param name="interface_transparency">interface transparency</param>
        public override void draw_post_processing(Engine engine, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(engine, interface_color, interface_transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.circular_progress:
                            {
                                // show percentage value
                                if (show_value)
                                {
                                    float percent = engine.get_percentage_of_range(min_value, max_value, tracked_id);
                                    percent = percent > 1f ? 1f : percent; // clip to 100% (if larger)
                                    percent = percent < 0f ? 0f : percent; // clip to 0% (if smaller)
                                    percent *= 100f; // change to a readable format

                                    Vector2 cur_sl_text = engine.get_UI_font().MeasureString(percent.ToString() + " %");
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, cur_sl_text, orientation.both);
                                    engine.xna_draw_outlined_text(percent.ToString() + " %", draw_origin, Vector2.Zero, Color.Black, Color.WhiteSmoke, engine.get_UI_font());
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default

            temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.circular_progress)); // circular progress chart will be fully contained in 1 sector

            zones.Add(temp_cb);
        }


        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            //base.draw(engine, color, transparency);

            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {// rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    // draw an element in calculated position
                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.circular_progress:
                            {
                                // define drawing sequence here
                                // background and progress bar
                                int min = Convert.ToInt32(min_value);
                                int max = Convert.ToInt32(max_value);
                                int curr = Convert.ToInt32(tracked_id);
                                float percent = engine.get_percentage_of_range(min, max, curr);
                                percent = percent > 1f ? 1f : percent; // clip to 100% (if larger)
                                percent = percent < 0f ? 0f : percent; // clip to 0% (if smaller)

                                Vector2 draw_origin = engine.vector_centered(draw_zone, half_circle.Bounds, orientation.both);
                                Vector2 rotation_adjustment = new Vector2(half_circle.Width / 2, half_circle.Height / 2); // required due to scaling

                                // show backing circle
                                Vector2 bg_draw_origin = engine.vector_centered(draw_zone, background_circle.Bounds, orientation.both);
                                Vector2 bg_draw_adjustment = new Vector2(background_circle.Width / 2, background_circle.Height / 2); // required due to scaling
                                // draw backing plate here (scale needs to be slightly larger to create a border)
                                engine.xna_draw(background_circle, bg_draw_origin + bg_draw_adjustment, null, backing_plate_color, 0f, new Vector2(background_circle.Width / 2, background_circle.Height / 2), ((float)(bounds.Width + 10) / (float)graphic_quality_precision), SpriteEffects.None, 1f); // background circle drawn in a dark grey
                                // drawing half_circles based on percentage
                                if (percent <= 0.5f) // draw anything less than 50% (difficult to desing...)
                                {
                                    engine.xna_draw(half_circle, draw_origin + rotation_adjustment, null, progress_color, (float)Math.PI * (percent / 0.5f), new Vector2(half_circle.Width / 2, half_circle.Height / 2), (float)(bounds.Width) / (float)graphic_quality_precision, SpriteEffects.None, 1f); // right half_circle
                                    // draw a half-circle in background color to negate overflowing progress half-circle
                                    engine.xna_draw(half_circle, draw_origin + rotation_adjustment, null, backing_plate_color, 0f, new Vector2(half_circle.Width / 2, half_circle.Height / 2), (float)(bounds.Width + 2) / (float)graphic_quality_precision, SpriteEffects.None, 1f); // right half_circle
                                }
                                else
                                {
                                    engine.xna_draw(half_circle, draw_origin + rotation_adjustment, null, progress_color, (float)Math.PI, new Vector2(half_circle.Width / 2, half_circle.Height / 2), (float)bounds.Width / (float)graphic_quality_precision, SpriteEffects.None, 1f); // right half_circle
                                    engine.xna_draw(half_circle, draw_origin + rotation_adjustment, null, progress_color, (float)Math.PI * 2 * percent, new Vector2(half_circle.Width / 2, half_circle.Height / 2), (float)bounds.Width / (float)graphic_quality_precision, SpriteEffects.None, 1f); // left half_circle (> 50%)
                                }

                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // element specific functions below
        public void set_element_values(int min_value, int max_value, int current = 0)
        {
            this.min_value = min_value;
            this.max_value = max_value;
            this.tracked_id = current;
        }

        public void update(int value)
        {
            this.tracked_id = value;
        }

    }
    //------------------------------------------------------------------------------------------------------------------
    [Serializable()]
    public class TextInput : UIElementBase // action = focus_input >> makes this element receive input 
    {
        string input_text; // this is input text currently in this element
        string target; // where will the input go - GUI element id
        bool focused; // this input element is focused
        const int MAX_INPUT_LENGTH = 300; // how many characters are allowed in one message (testing at 30 characters)
        const int OFFSET = 4; // horizontal offset from right/left for the text (does not account for border width)
        [NonSerialized]
        Texture2D border;
        const string PLACEHOLDER = "type here...";

        public TextInput(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            input_text = "";
            focused = false;
            create_sectors();
            set_border_texture();
        }
        // Template section
        /// <summary>
        /// ISectored interface function - creates zones inside the element. There has to be at least one horizontal zone containing at least one vertical zone to make a valid element
        /// </summary>
        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default

            temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.text_input_display)); // input text string will be fully contained in 1 sector

            zones.Add(temp_cb);
        }
        /// <summary>
        /// Main Element rendering <<<<<<<<<<<<<<<<<<<<<<<<
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="color"></param>
        /// <param name="transparency"></param>
        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            if (focused || current_state == state.default_state)
            {
                base.draw(engine, Color.Black, transparency); // draw text background in Black
            }
            else if (current_state == state.hovered)
            {
                base.draw(engine, new Color(40, 40, 40), transparency); // draw text background in Lighter gray
            }

            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                            {
                                int max_length = v.get_width() - (OFFSET * 2); // double offset = both sides
                                Vector2 text = engine.get_UI_font().MeasureString(input_text);

                                if (input_text.Length == 0)
                                {
                                    Vector2 textp = engine.get_UI_font().MeasureString(PLACEHOLDER);
                                    // placeholder
                                    if (!focused)
                                    {
                                        Vector2 draw_origin = engine.vector_centered(draw_zone, textp, orientation.vertical_left) + new Vector2(OFFSET, 0);
                                        engine.xna_draw_text(PLACEHOLDER, draw_origin, Vector2.Zero, Color.DarkGray, engine.get_UI_font());
                                    }
                                }
                                else if (text.X > max_length) // text too long - crop from the right edge to max length
                                {
                                    Rectangle crop = new Rectangle(draw_zone.X, draw_zone.Y, v.get_width() - OFFSET * 2, h.get_height());
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, text, orientation.vertical_left) + new Vector2(OFFSET, 0);
                                    draw_origin.X = draw_zone.X - ((int)text.X - max_length); // reset to beginning

                                    if (focused)
                                        engine.xna_draw_text_crop_ui(input_text, draw_origin, Vector2.Zero, Color.White, engine.get_UI_font(), crop);
                                    else
                                        engine.xna_draw_text_crop_ui(input_text, draw_origin, Vector2.Zero, Color.SlateGray, engine.get_UI_font(), crop);
                                }
                                else // normal display
                                {
                                    Vector2 draw_origin = engine.vector_centered(draw_zone, text, orientation.vertical_left) + new Vector2(OFFSET, 0); // align text to left edge and move it 2 pixels to the right

                                    if (focused)
                                        engine.xna_draw_text(input_text, draw_origin, Vector2.Zero, Color.White, engine.get_UI_font());
                                    else
                                        engine.xna_draw_text(input_text, draw_origin, Vector2.Zero, Color.SlateGray, engine.get_UI_font());
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Masking rendering for shader effect
        /// </summary>
        /// <param name="engine"></param>
        public override void draw_masking_sprite(Engine engine)
        {
            base.draw_masking_sprite(engine);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                            {

                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Post Processing rendering
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="interface_color"></param>
        /// <param name="interface_transparency"></param>
        public override void draw_post_processing(Engine engine, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(engine, interface_color, interface_transparency);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                            {
                                // draw "border" texture at a standard origin vector
                                Vector2 draw_origin = engine.vector_centered(draw_zone, border.Bounds, orientation.both);
                                // draw slider line
                                engine.xna_draw(border, draw_origin, null, interface_color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // draws border in current interface color
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        // Function section
        public void set_border_texture()
        {
            // use create_colored_hollow_rectangle function 
            border = Game1.create_colored_hollow_rectangle(bounds, Color.White, 1f, 2); // 2 pixel border
        }
        /// <summary>
        /// Removes current input text from this element and creates an empty string
        /// </summary>
        public void clear_text()
        {
            input_text = "";
        }
        /// <summary>
        /// Focuses or unfocuses current input 
        /// </summary>
        /// <param name="value">boolean for focus status desired</param>
        public void set_focus(bool value)
        {
            focused = value;
        }

        public void add_text(string value)
        {
            if (input_text.Length < MAX_INPUT_LENGTH)
                input_text = String.Concat(input_text, value); // adds a character to input         
        }

        public string get_last_input()
        {
            if (input_text.Length > 0)
                return input_text[input_text.Length - 1].ToString();
            else
                return "";
        }

        public void erase_one_character()
        {
            if (input_text.Length > 0)
            {
                string temp = input_text.Substring(0, input_text.Length - 1);
                input_text = temp;
            }
        }
        public void set_input_target(string tgt)
        {
            target = tgt;
        }
        public string get_input_target_id()
        {
            return target;
        }

        public string get_text()
        {
            return input_text;
        }
    }
    //------------------------------------------------------------------------------------------------------------------
    [Serializable()]
    class TextArea : UIElementBase
    {
        private List<string> tnp = new List<string>(); // temporary list
        
        public TextArea(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
            : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
        {
            create_sectors();
        }

        public new void create_sectors()
        {
            horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default

            temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.text_area)); // circular progress chart will be fully contained in 1 sector

            zones.Add(temp_cb);
        }

        public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            // replace base draw 
            if (background != null) // Text Area Background HERE
                engine.xna_draw(get_background(), get_origin(), null, Color.Black*0.75f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9f);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_area:
                            {
                                // draw "border" texture at a standard origin vector
                                //Vector2 draw_origin = engine.vector_centered(draw_zone, border.Bounds, orientation.both);
                                // draw slider line
                                //gine.xna_draw(border, draw_origin, null, interface_color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f); // draws border in current interface color
                                int counter = 0;
                                foreach (string s in tnp) // testing drawing text in area
                                {
                                    Vector2 dimensions = engine.get_UI_font().MeasureString(s);
                                    Vector2 draw_origin = engine.rectangle_to_vector(draw_zone); // no centering
                                    //Vector2 draw_origin = engine.vector_centered(draw_zone, dimensions, orientation.horizontal);
                                    engine.xna_draw_text(s, draw_origin + new Vector2(0, counter), Vector2.Zero, engine.inverted_color(color), engine.get_UI_font());
                                    counter += 20;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void draw_masking_sprite(Engine engine)
        {
            base.draw_masking_sprite(engine);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_area:
                            {
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void draw_post_processing(Engine engine, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(engine, interface_color, interface_transparency);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_area:
                            {
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Function to test input from another element
        /// </summary>
        /// <param name="val">value of the string passed through</param>
        public void accept_text_output(string val)
        {
            tnp.Add(val);
        }
    }
}// end namespace


/* BASIC UI ELEMENT TEMPLATE 
class ElementNameHere : UIElementBase
{
    public ElementNameHere(string id, Container parent, type f, actions? c, confirm safety, Rectangle dimension, Texture2D icon, String label, String tooltip)
        : base(id, parent, f, c, safety, dimension, icon, label, tooltip)
    {
        create_sectors();
    }

    public new void create_sectors()
    {
        horizontal_sector temp_cb = new horizontal_sector(0, bounds.Height);     // create default

        temp_cb.add_vertical(new vertical_sector(0, bounds.Width, sector_content.circular_progress)); // circular progress chart will be fully contained in 1 sector

        zones.Add(temp_cb);
    }

public override void draw(Engine engine, Color color, float transparency = -1f)
        {
            base.draw(engine, color, transparency);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                            {
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void draw_masking_sprite(Engine engine)
        {
            base.draw_masking_sprite(engine);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                        {
                        }
                        break;
                        default:
                        break;
                    }
                }
            }
        }

        public override void draw_post_processing(Engine engine, Color interface_color, float interface_transparency)
        {
            base.draw_post_processing(engine, interface_color, interface_transparency);
            // element specific functionality
            foreach (horizontal_sector h in zones)
            {
                foreach (vertical_sector v in h.get_verticals())
                {   // rendering sequence - v contains x_start and x_end of the rectangle but y_start and y_end are in the "h"
                    // calculate current vertical sector rectangle in on-screen coordinates based on "h" and "v" sectors and Unit origin(x,y)
                    Rectangle draw_zone = new Rectangle(v.get_xs() + (int)get_origin().X, h.get_ys() + (int)get_origin().Y, v.get_width(), h.get_height());

                    switch (v.get_content_type()) // switch sector selection
                    {
                        case sector_content.text_input_display:
                            {
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
}
 */