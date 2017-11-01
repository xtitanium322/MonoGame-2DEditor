using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{
    /*Text rendering Engine - display system quest and other game messages in specified containers (UI elements with a Rectangle defined for text).
            Suports randomly colored and highlighted words in a message.

            1. define a message element
            2. define a display engine

            Example of encoded text message for TextEngine to process
            Create a library of text messages that can be copied and added to engine
            */

    /*-------------------------------------------------------------------------------------------------------------------------------------------*/
    // a message will be broken out into text elements according to its encoding
    // collect only one word in order to be able to break messages into lines smoothly
    enum decode_mode { none, variable, color, action };

    /*
      Usage: List<text_element> lst; // creates a list of text elements to be used in the message element
    */
    public struct text_element
    {
        Color text_color;
        //string text_color_surrogate; // for serialization
        string text;
        SpriteFont text_font;

        Rectangle text_rectangle; // calculate from resulting texture and Font used
        //Texture2D text_texture;   // will have to be created

        public text_element(SpriteFont font, Color color, string message_text)
        {
            text_color = color;
            text = message_text;
            text_font = font;
            text_rectangle = Rectangle.Empty;
            //text_texture = null;
        }

        public void initialize()
        {
            // create a texture
            // calculate rectangle
        }

        public void set_text(string value)
        {
            text = value;
        }

        public void set_color(Color c)
        {
            text_color = c;
        }

        public void set_text_font(SpriteFont f)
        {
            text_font = f;
        }

        public string get_text()
        {
            return text;
        }

        public Color get_color()
        {
            return text_color;
        }

        public SpriteFont get_font()
        {
            return text_font;
        }

        public void append(string val)
        {
            text += val;
        }
    }
    /*-------------------------------------------------------------------------------------------------------------------------------------------*/
    // text elements will be collected into a single message
    /*
      Usage: List<message_element> messages; // create a list of all messages being used by a text engine class.
      Go through the list of messages and display in a text area decoded.
    */
    public struct message_element
    {
        List<text_element> message; // entire message
        List<int> line_indexes; // stores order number of each line-breaking word
        string message_tag; // a unique id for this message so it can be found later
        int timestamp; // this value should be a 0 on creation and is only used when message is added to any engine object

        public message_element(string tag)
        {
            message = new List<text_element>();
            line_indexes = new List<int>();
            message_tag = tag;
            timestamp = 0;
        }

        public void add_text_element(text_element a)
        {
            message.Add(a);
        }
        public List<text_element> get_message()
        {
            return message;
        }
        public void set_timestamp(int value)
        {
            timestamp = value;
        }

        public string get_message_tag()
        {
            return message_tag;
        }
        // calculate how many lines will this message represent in a given text area. Assign starting text_element number to each line and store in the index list
        public int calculate_lines(Engine engine, Rectangle text_area, int padding = 0)
        {
            int max = text_area.Width - (padding * 2); // current width for drawing
            int temp = 0;
            int lines = 1;
            // create a texture for every word given it's color and add it's width to temp
            foreach (text_element t in message)
            {
                Vector2 text_length = engine.get_UI_font().MeasureString(t.get_text());

                if (temp + (int)text_length.X <= max)
                {
                    temp += (int)text_length.X;
                }
                else
                {
                    temp = (int)text_length.X;
                    lines++;
                }
            }
            return lines;
        }
    }

    /*-------------------------------------------------------------------------------------------------------------------------------------------*/
    // shows all the messages that fit into target area, then crops the oldest ones and any scrolled messages
    // scrolling will decide which line is the anchor at the bottom of the chat by manipulating a Vector2 draw_origin
    //	if 1 - origin is not offset down, if 2 - origin is moved 1 line down and messages get truncated from both sides using a shader. a scrollbar will appear
    // text area will have a scrollbar updated based on line numbers. it will appear when total lines > available lines. Copy logic from tile selection menu
    // messages can either be generated by the system in response to some event or typed in directly by the user and then get decoded by create_message_element function. In this case, user can use tags for variables or colors to customize message.
    // Commands typed in by the user will go through decoder of TextEngine but will also go to command execution center. Commands must start with a _ and have a correct number and type of arguments
    // Add usage examples in optimization
    public class TextEngine
    {
        Queue<message_element> messages; // list of all active messages that have to be drawn by this object, e.g. a chat textengine, a quest window textengine
        //int starting_line; 			     // if chat area is scrolled this will change. default is 1. The newest message will get pushed down out of the rectangle. max value for this = total lines - text area lines. Offset will be calculated as follows: new Vector2(0, (starting_line-1) * line height);
        const int MAX_MESSAGES = 20;	  // number of messages to keep. If max is reached - dequeue the oldest message before adding a new one
        Rectangle target_bounds; // values specifying target text area
        Vector2 target_origin;
        Color standard_color; // this color will be used for all words that dont have specific color encoding.  This value gets assigned to any text_element generated here
        SpriteFont engine_font; // font used to display all messages by this engine. This value gets assigned to any text_element generated here
        List<List<text_element>> line_list;
        List<List<text_element>> small_line_list; // a copy for display
        int padding = 5;
        int line_height = 20;

        public TextEngine(SpriteFont std_font, Rectangle text_area, Vector2 text_area_origin, Color std_color)
        {
            messages = new Queue<message_element>(MAX_MESSAGES);
            line_list = new List<List<text_element>>();
            small_line_list = new List<List<text_element>>();
            target_bounds = text_area;
            target_origin = text_area_origin;
            standard_color = std_color;
            engine_font = Game1.small_font;
        }

        public void scroll(bool up)
        {
            int max_lines = target_bounds.Height / line_height;
            if (up)
            {
                //if max available lines - area lines == curent starting line ignore
                //if(line_list.Count - max_lines == starting_line)
                // otherwise - increase starting line value
            }
            else
            {
                // if current starting line == 1 - ignore
                // otherwise decrement current value
            }
        }
        // add a message to chat queue of this Engine object
        public void add_message_element(message_element a, int current_time)
        {
            if (messages.Count == MAX_MESSAGES)
                messages.Dequeue(); // remove oldest

            a.set_timestamp(current_time); //current ms value
            messages.Enqueue(a);
        }

        public void add_message_element(Engine engine, string input_text)
        {
            if (messages.Count == MAX_MESSAGES)
                messages.Dequeue(); // remove oldest

            message_element temp = create_message_element(engine, input_text);
            messages.Enqueue(temp);
        }

        public void set_font(SpriteFont font)
        {
            engine_font = font;
        }
        // grab a message from *dictionary*
        public message_element? get_message(string tag)
        {
            foreach (message_element a in messages)
            {
                if (a.get_message_tag() == tag)
                    return a;
            }

            return null;
        }

        /// <summary>
        /// Updates message lists
        /// </summary>
        public void update()
        {
            int cutoff_length = target_bounds.Width - (padding * 2);
            convert_to_ordered_lines(line_list, cutoff_length); // requires optimization

            int max_lines = target_bounds.Height / line_height;

            // truncate list for non-shader masked rendering (does not support scrolling yet)
            if (line_list.Count > max_lines)
            {
                small_line_list = line_list.GetRange(0, max_lines).ToList();
            }
            else
            {
                small_line_list = line_list;
            }
        }
        public void clear_messages()
        {
            messages.Clear();
        }
        public void set_target(Rectangle target_area, Vector2 origin)
        {
            target_bounds = target_area;
            target_origin = origin;
        }

        // decoder will have to calculate each frame due to variables changing - example a time stamp in ms or current time of day
        // this function creates a rendereable message_element based on the coded string containing such codes as text color and variable names. Variable and color codes must not have spaces in them.
        // Design of this function is completed
        // other example messages:
        /*
        "~CURRENT_TIME[16,0,200] system:[16,200,200] Light has been added to ~CURRENT_CELL[0,255,0]"  << IMPORTANT: here the value for color can be auto-generated based on system color dictionary values. look up by system color type and append a formatted value in string decoder format
        "~CURRENT_TIME[16,0,200] system:[16,200,200] Mode changed to ~CURRENT_EDIT_MODE[0,255,255]" << example system message for changed editor mode
        "~CURRENT_TIME[16,0,200] system:[16,200,200] SubMode changed to ~CURRENT_EDIT_SUBMODE[0,255,255]" << example system message for changed editor submode
        "{one two three}[red] - this message will have all words colored the same"
        */

        /// <summary>
        /// Updated version of the message creator
        /// step 1. resolve all variables and substitute them with new text, updated text string will go through step 2
        /// step 2. create text elements, assigning color values to each one
        /// </summary>
        /// <param name="e">Engine</param>
        /// <param name="encoded_string">a raw string of the message with variables and other tags</param>
        /// <returns></returns>
        public message_element create_message_element(Engine e, string encoded_string)
        {
            message_element result = new message_element("decoder message");
            text_element temp_text = new text_element();
            List<text_element> grouped = new List<text_element>();             // a temporary group used to assign the same coloor to multiple text messages , retroactively
            bool grouping = false;
            string temp = "";
            string encoded_action = "";

            string t = encoded_string;                   // initial message that hasn't been processed yet
            t = String.Concat(t, ' '); 					 // adds a space signaling the end of last word
            Color current_encoder = standard_color; 	 // initialize with standard color
            decode_mode dm = decode_mode.none; 			 // default decoder mode

            // step 1 - decode variables
            t = replace_variables(e, t) + " ";// in case the variable has no space at the end (might cause missing text)

            // step 2 - decode colors
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] == '[')
                {
                    // stop writing to temp
                    if (dm == decode_mode.none)
                    {
                        temp_text.set_text(temp); // assign collected text if its not a variable
                        temp = ""; // reset temp string
                    }

                    // start color decoding
                    dm = decode_mode.color; // set decode mode color
                }
                else if (t[i] == ']') // signals the end of color mode
                {
                    // depedning on decode mode a different kind of message element will be generated
                    switch (dm)
                    {
                        case (decode_mode.variable): // no functionality
                            break;
                        case (decode_mode.color): // assigns color to encoder
                            try
                            {
                                current_encoder = delimited_string_to_color(temp); //NEEDS try-catch block implemented in case user enters a wrong format of color../ convert current color in temp string to a real value and assigned to current encoded color
                            }
                            catch
                            {
                                current_encoder = Color.White;
                            }
                                                       
                            break;
                        case (decode_mode.none): // no functionality
                            break;
                        default:
                            break;
                    }
                    // create text element
                    temp_text.set_color(current_encoder);       // can vary
                    temp_text.set_text_font(Game1.small_font);  // font size
                   
                    // for all grouped messages - color them now
                    if(grouped.Count > 0)
                    {
                        foreach(text_element te in grouped)
                        {
                            te.set_color(current_encoder);
                            result.add_text_element(te);
                        }

                        grouped.Clear();
                    }
                    // after adding all the grouped messages - finish up with the most recent one
                    result.add_text_element(temp_text);         // ADD text element to result
                    // clean-up
                    dm = decode_mode.none;                      // reset decode mode
                    current_encoder = standard_color;           // reset in case it was changed by color tag of coded message
                    temp = ""; //reset temp string again

                    temp_text = new text_element();             // since it was copied over a new structure can be created
                }
                else if(t[i] == '{')
                {
                    grouping = true;
                }
                else if(t[i] == '}') // {~fps is this}[red]
                {
                    grouping = false;
                }
                else if (t[i] == '*')
                {
                    dm = decode_mode.action;                    // this new mode allows for parameters after the command
                }
                else if (t[i] == ' ')// at this point a text_element is completed and can be added to message_element
                {
                    //Debug.WriteLine(encoded_action);
                    if (dm != decode_mode.action)
                    {
                        if (temp.Length > 0) // make sure there is something to add before creating a text element
                        {
                            // temp_text.text has already been setup by this point if current mode is color
                            if (dm == decode_mode.none)
                                temp_text.set_text(temp);

                            // create text element
                            temp_text.set_color(current_encoder);       // can vary
                            temp_text.set_text_font(Game1.small_font);  // font size


                            if (grouping)
                                grouped.Add(temp_text);
                            else
                            {
                                if (grouped.Count > 0)
                                {
                                    foreach (text_element te in grouped)
                                    {
                                        te.set_color(current_encoder);
                                        result.add_text_element(te);
                                    }

                                    grouped.Clear();
                                    // clean up
                                    temp_text.set_text(temp);
                                    result.add_text_element(temp_text);         // ADD text element to result
                                }
                                else
                                {
                                    temp_text.set_text(temp);
                                    result.add_text_element(temp_text);         // ADD text element to result
                                }
                            }

                            // clean-up
                            dm = decode_mode.none;                      // reset decode mode
                            current_encoder = standard_color;           // reset in case it was changed by color tag of coded message
                            temp = ""; //reset temp string again

                            temp_text = new text_element();             // since it was copied  over a new structure can be created
                        }
                    }
                    else
                    {
                        // perform an actions specified by the user command typed in system input
                        string answer = perform_action(e, encoded_action, t.Substring(i)); // run the action on the remaining string which should have only the parameters required for the action and get response
                        temp_text.set_color(Color.OrangeRed);
                        temp_text.set_text_font(Game1.small_font);     // font size
                        temp_text.set_text(answer);
                        

                        if (grouping)
                            grouped.Add(temp_text);
                        else
                            result.add_text_element(temp_text);

                        return result; // end the function
                    }
                }
                else // any other valid character or number will simply be appended to temporary string
                {
                    if (dm != decode_mode.action)
                    {
                        temp = String.Concat(temp, t[i]); // write a character to current temp string
                    }
                    else if (dm == decode_mode.action)
                    {
                        encoded_action = String.Concat(encoded_action, t[i]); // write a character to encoded action string
                    }
                }
            }
            // done
            return result;
        }
        /// <summary>
        /// Performs an action based on user input
        /// </summary>
        /// <param name="e">engine needed for access to world and other important objects</param>
        /// <param name="action">name of the command</param>
        /// <param name="ps">substring with relevant parameters</param>
        /// <returns>string to display in system chat</returns>
        public string perform_action(Engine e, string action, string ps)
        {
            string par1 = "";

            if (action.Equals("st"))
            {
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i] != ' ')
                    {
                        par1 = String.Concat(par1, ps[i]);
                    }
                    else if (ps[i] == ' ')
                    {
                        // skip
                        if (par1.Length == 0)
                        {
                            continue;
                        }
                        else
                        {
                            int time;

                            if (Int32.TryParse(par1, out time))
                            {
                                // set the in-game clock
                                if (time >= 0 && time <= 23)
                                {
                                    e.clock.set_clock(time, 0);
                                    return "*st " + time.ToString() + ":00 success";
                                }
                                else
                                    return time + " is incorrect value for *st";
                            }
                            else
                            {
                                return "wrong parameters for *st";
                            }
                        }
                    }
                }
                return "<bad command format>"; // if there is no space at the end of the string
            }
            else if (action.Equals("brush"))
            {
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i] != ' ')
                    {
                        par1 = String.Concat(par1, ps[i]);
                    }
                    else if (ps[i] == ' ')
                    {
                        // skip
                        if (par1.Length == 0)
                        {
                            continue;
                        }
                        else
                        {
                            int size;

                            if (Int32.TryParse(par1, out size))
                            {
                                // set the in-game clock
                                if (size >= 0 && size <= 10)
                                {
                                    e.get_editor().set_brush_size(size);
                                    return "brush size changed to " + size;
                                }
                                else
                                    return size + " is incorrect value for *brush";
                            }
                            else
                            {
                                return "wrong parameters for *brush";
                            }
                        }
                    }
                }
                return "<bad command format>"; // if there is no space at the end of the string
            }
            else if (action.Equals("stats"))
            {
                //Game1.toggle_stats(); // turns statistic display on or off
                // make stats container visible invisible
                e.get_editor().GUI.find_container("CONTAINER_STATISTICS_TEXT_AREA").set_visibility("toggle");
                return "statistic display toggled";
            }
            else if(action.Equals("gl")) // generate lights
            {
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i] != ' ')
                    {
                        par1 = String.Concat(par1, ps[i]);
                    }
                    else if (ps[i] == ' ')
                    {
                        // skip
                        if (par1.Length == 0)
                        {
                            continue;
                        }
                        else
                        {
                            int size;

                            if (Int32.TryParse(par1, out size))
                            {
                                // set the in-game clock
                                if (size >= 0 && size <= 1000) // currently takes a second per 100 lights - will create a smoother functionality that doesnt put all lights in one frame, but rather limited number per frame, so that drawing can continue while lights are created
                                {
                                    // generate lights
                                    // instead of generating them all in this frame, just add a number to light generating queue
                                    // each frame this queue will generate a limited amount of lights until number is exhausted
                                    e.order_light_generation(size);
                                    // report success
                                    return "generated " + size + " lights in random positions";
                                }
                                else
                                    return size + " is incorrect parameter for *gl /nl maximum = 1000";
                            }
                            else
                            {
                                return "wrong parameters for *gl";
                            }
                        }
                    }
                }
                return "<bad command format>"; // if there is no space at the end of the string
            }
            else if(action.Equals("lc"))
            {
                // switches light colors for all the lights currently in selection matrix
                // only several values are available
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i] != ' ')
                    {
                        par1 = String.Concat(par1, ps[i]);
                    }
                    else if (ps[i] == ' ')
                    {
                        // skip
                        if (par1.Length == 0)
                        {
                            continue;
                        }
                        else
                        {
                            List<PointLight> l = e.get_editor().get_selection_lights();

                            if (par1.Equals("red"))
                            {   
                                // switch the light color
                                foreach(PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Red);
                                }
                            }
                            else if(par1.Equals("green"))
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Green);
                                }
                            }
                            else if (par1.Equals("blue"))
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Blue);
                                }
                            }
                            else if (par1.Equals("yellow"))
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Yellow);
                                }
                            }
                            else if (par1.Equals("violet"))
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Violet);
                                }
                            }
                            else if (par1.Equals("white"))
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.White);
                                }
                            }
                            else if (par1.Equals("transparent")) // black color lightens up the textures behind it but gives no color to tint the scenery, so it's the most natural lighting possible
                            {
                                // switch the light color
                                foreach (PointLight pl in l)
                                {
                                    pl.change_light_color(Color.Black);
                                }
                            }
                            else
                            {
                                return "wrong parameters for *lightcolor, use red,green, blue, yellow, violet or white";
                            }
                            return "light color switched to " + par1;
                        }
                    }
                }

                return "<bad command format>";
            }
            else if (action.Equals("lightsoff"))
            {
                e.get_current_world().destroy_lights();
                return "light emitters destroyed";
            }
            else if (action.Equals("wateroff"))
            {
                e.get_current_world().destroy_water_generators();
                return "water generators destroyed";
            }
            else
            {
                return "<undefined action>";
            }
        }
        /// <summary>
        /// Replaces ~variables in the string
        /// </summary>
        /// <returns>updated string</returns>
        public string replace_variables(Engine e, string t)
        {
            string out_string = "";           // contains complete string with resolved variables
            string var_name = "";           // contains variable name, will be resolved by decode_variable()
            bool building_variable = false;   // switch the mode

            // read the string
            for (int i = 0; i < t.Length; i++)
            {
                // variable not detected - build the output string
                if (!building_variable)
                {
                    if (t[i] == '~') // variable identifying symbol 
                    {
                        building_variable = true; // mode has been switched and the character doesn't have to be duplicated in the output string
                    }
                    else
                    {
                        out_string = String.Concat(out_string, t[i]); // normal mode - build the output string
                    }
                }
                // variable detected - switch to building variable name which will later be replaced
                else
                {
                    // variable name ends
                    if (t[i] == '[' || t[i] == ' ')
                    {
                        out_string = String.Concat(out_string, decode_variable(e, var_name)); // add decoded result to the overall result and continue building the string
                        building_variable = false;            // reset for the rest of the string                      
                        var_name = "";

                        if(t[i] != ' ')
                            out_string = String.Concat(out_string, t[i]); // concatenate the character as well
                    }
                    // variable name continues
                    else
                    {
                        var_name = String.Concat(var_name, t[i]);
                    }
                }
            }
            return out_string;
        }
        /// <summary>
        /// Replaces a variable with appropriate information. Custom color tags are added for several variables.
        /// </summary>
        /// <param name="e">Engine</param>
        /// <param name="variable_tag">variable name: e.g. ~fps </param>
        /// <param name="decoder_color">output color of the decoded string</param>
        /// <returns></returns>
        public string decode_variable(Engine e, string variable_tag)
        {
            string temp = "";
            if (variable_tag.ToUpper() == "MS") // based on the key - get the value of this variable
            {
                temp = "ms:[orange] " + e.get_current_game_millisecond().ToString() + "[" + color_to_delimited_string(Color.DarkGreen) + "]";
            }
            else if (variable_tag.ToUpper() == "CAM")
            {
                temp = "cam:[orange] offset = " + e.get_camera_offset().ToString();
            }
            else if (variable_tag.ToUpper() == "SIZE")
            {
                temp = "size:[orange] " + e.get_current_world().get_world_size().ToString() + "[violet]";
            }
            else if (variable_tag.ToUpper() == "FPS")
            {
                temp = "fps:[orange] " + e.get_fps().ToString() + "[skyblue]";
            }
            else if (variable_tag.ToUpper() == "TIME")
            {
                temp = Game1.thisDay.Hour.ToString("D2") + ":" + Game1.thisDay.Minute.ToString("D2") + "[skyblue]";
            }
            else if (variable_tag.ToUpper() == "H")
            {
                temp = "help(h):[orange] " + "~ms[orange] (show milliseconds since start), /nl ~colors[orange] (show colors available for the [] tag), /nl ~cam[orange] (show camera offset), /nl ~size[orange] (show total number of cells in the game world), /nl ~fps[orange] (show current fps), /nl ~time[orange] (show time) ";
            }
            else if (variable_tag.ToUpper() == "A")
            {
                temp = "actions(a):[orange] " + "{gl <0-100>}[orange] (generate point lights at random positions) /nl st[orange] <0-23> (set game world clock), /nl lc[orange] - change light color of selected light(s), /nl wateroff[orange] - destroy water generators, /nl lightsoff[orange]  - destroy light sources, /nl brush[orange]  <0-10>, /nl stats[orange]  - display or hide statistics ";
            }
            else if (variable_tag.ToUpper() == "COLORS")
            {
                temp = "colors:[orange] " + " red[red], /nl green[green], /nl blue[blue], /nl violet[violet], /nl orange[orange], /nl pink[pink], /nl yellow[yellow], /nl skyblue[skyblue]";
            }
            else if (variable_tag.ToUpper() == "TEST")
            {
                temp = "test[orange]";
            }
            else
            {
                temp = variable_tag + "[orange] is " + "undefined";
            }

            return temp;
        }

        // converts a string value to xna Color value
        public static Color delimited_string_to_color(string color_string)// format "int,int,int",e.g. 25,125,44
        {
            // check actual color names first
            if (color_string.Equals("green"))
            {
                return Color.Green;
            }
            else if (color_string.Equals("red"))
            {
                return Color.Red;
            }
            else if (color_string.Equals("blue"))
            {
                return Color.Blue;
            }
            else if (color_string.Equals("orange"))
            {
                return Color.Orange;
            }
            else if (color_string.Equals("pink"))
            {
                return Color.Pink;
            }
            else if (color_string.Equals("violet"))
            {
                return Color.BlueViolet;
            }
            else if (color_string.Equals("yellow"))
            {
                return Color.Yellow;
            }
            else if (color_string.Equals("skyblue"))
            {
                return Color.DeepSkyBlue;
            }

            // numeric color codes
            int r = 0;// color value placeholders
            int g = 0;
            int b = 0;

            string temp = ""; // a temp string to read in characters
            int order = 1; // which variable is being used

            try
            {
                for (int i = 0; i < color_string.Length; i++) // read character by character
                {
                    if (color_string[i] == ',')
                    {
                        // assign number and switch to the next variable
                        if (order == 1)
                        {
                            r = Int32.Parse(temp);
                        }
                        else if (order == 2)
                        {
                            g = Int32.Parse(temp);
                        }
                        else
                        {
                            b = Int32.Parse(temp);
                        }
                        temp = ""; // reset temp string
                        order++;   // increase color component order
                    }
                    else
                    {
                        temp = String.Concat(temp, color_string[i]);
                    }
                }
                b = Int32.Parse(temp); // collects the last number
            }
            catch (FormatException)
            {
                return Color.White; // return a standard white color if encoding is wrong
            }

            return new Color(r, g, b);
        }
        /// <summary>
        /// Creates a comma delimited string based on color r g and b values.
        /// </summary>
        /// <param name="color">Source color</param>
        /// <returns>delimited string value</returns>
        public static string color_to_delimited_string(Color color)
        {
            string color_val = color.R + "," + color.G + "," + color.B;
            return color_val;
        }
        ///<summary>
        /// Draws messages in a rectangular area. Messages appear chronologically as older on top, newer on the bottom of the area.
        ///</summary>
        /// <param name="source">message_element list - a collection of messages to be written in an area</param>
        /// <param name="area">A rectangle on screen where message should go</param>
        /// <param name="line_height">A value added to origin vector every line change</param>
        /// <param name="padding">A pixel value to indent text from each side</param>
        public void textengine_draw(Engine engine, bool top_to_bottom = false)
        {// first-in-first-out = oldest 1st
            //int area_lines = area.Height/line_height; // NOT NEEDED - overflow will be culled using a specialized masking shader truncates floating point - number of lines supported by the Rectangle
            if (messages.Count > 0)
            {
                int current_line = 1; // top-to-bottom offset added to original vector (in or outside of screen space)
                int horizontal_offset = padding;
                int cutoff_length = target_bounds.Width - (padding * 2);

                //convert_to_ordered_lines(line_list, cutoff_length); // needs optimization (create a class variable that gets reset at the end of each draw cycle and rebuilt at the start of another)

                Vector2 main_origin;
                // calculate origin of the text display
                if(!top_to_bottom)
                    main_origin = target_origin + new Vector2(0, target_bounds.Height) - new Vector2(0, small_line_list.Count * line_height); // find origin by subtracting total height of all messages from target origin's lowest line
                else
                    main_origin = target_origin + new Vector2(0, (current_line-1) * line_height);

                small_line_list.Reverse(); // reverse for chronological order

                foreach (List<text_element> line in small_line_list)
                {
                    // specify drawing logic  (keep track of origin vector, line height offset and horizontal offset)
                    foreach (text_element t in line)
                    {
                         Vector2 draw_origin;
                        // set current origin vector
                        if(!top_to_bottom)
                            draw_origin = main_origin + new Vector2(horizontal_offset, (current_line - 1) * line_height);
                        else
                            draw_origin = main_origin + new Vector2(horizontal_offset, (current_line - 1) * line_height);
                        // draw text according to it`s color and any other values
                        Vector2 text_length = engine.get_UI_font().MeasureString(t.get_text());
                        engine.xna_draw_text(t.get_text(), draw_origin, Vector2.Zero, t.get_color(), t.get_font()); // using text and text_color variables in text_element t
                        // update horizontal offset
                        horizontal_offset += (int)text_length.X;
                    }
                    // once done - increment current_line and reset horizontal offset
                    current_line++;
                    horizontal_offset = padding;
                }

                // clear line_list once done
                //line_list.Clear();
            }
        }
        // delete get_message_line_indexes, is a linebreak, calculate_total_lines_in_queue functions
        // Optimize and add scrolling. Limit message number. 
        // total number of lines in message queue can be derived from Count poroperty of the converted list. converted.Count = total number of lines in message queue based on target rectangle bounds and padding 
        public void convert_to_ordered_lines(List<List<text_element>> converted, int max_length)
        {
            line_list.Clear(); // remove any elements to generate a fresh list in case anything changed (max_length)
            List<List<text_element>> message = new List<List<text_element>>(); // create a temporary message/line structure - holds a whole message separated into lines.  
            List<text_element> line = new List<text_element>();                // create a temporary line    
            float current_length = 0;


            //optimization: convert to for loops?
            //foreach (message_element m in messages)
            for (int i = 0; i < messages.Count; i++)
            {
                message_element m = messages.ElementAt(i);
                //foreach (text_element t in m.get_message())
                for (int j = 0; j < m.get_message().Count; j++)
                {
                    text_element t = m.get_message().ElementAt(j);

                    //determine if there is a line break keyword /nl in the text element
                    if (t.get_text() != null && t.get_text().Equals("/nl"))
                    {
                        // create a line break
                        message.Add(line);         // add line to temporary list of lines
                        line = new List<text_element>();   // create a new line
                        current_length = 0;
                        continue;
                    }

                    t.set_text(t.get_text() + " "); // add a space to separate words when rendered

                    float length = engine_font.MeasureString(t.get_text()).X;

                    if (current_length + length > max_length) // split and retry
                    {
                        text_element splitmessage = t;

                        while (engine_font.MeasureString(splitmessage.get_text()).X > (max_length - current_length))
                        {
                            int split_length = (int)(max_length - current_length);
                            var result = split_message(splitmessage, split_length); // creates a short message to fill the rest of this line and the remainder message that will be fed back into the system
                            result[0].append("-"); // mark a line end
                            line.Add(result[0]); // add the line filler 
                            message.Add(line);
                            line = new List<text_element>();
                            current_length = 0;

                            splitmessage = result[1];
                        }
                        // catch overflow remainder
                        line.Add(splitmessage);
                        current_length += (int)engine_font.MeasureString(splitmessage.get_text()).X;
                    }
                    else // normal situation
                    {
                        line.Add(t);
                        current_length += length;
                    }
                }
                // create a line break when a message element ends as well to separate each message in queue properly
                message.Add(line);         // add line to temporary list of lines
                line = new List<text_element>();   // create a new line
                current_length = 0;
            }
            // when a message is completed - add current non completed line if it exists, then reverse the order and append to main list using AddRange
            if (line.Count > 0)
                message.Add(line);

            message.Reverse();
            converted.AddRange(message);
        }
        /// <summary>
        /// Splits message in two parts. Element 0 = fills the rest of the line and adds - at the end, element 1 = remainder to be processed again
        /// </summary>
        /// <param name="t">non-split message</param>
        /// <param name="width">Number of pixels that has to equal string width in engine font</param>
        /// <returns>an array of text elements</returns>
        public text_element[] split_message(text_element t, int length)
        {
            text_element[] result = new text_element[2];
            string full_text = t.get_text();

            for (int i = full_text.Length - 1; i > 0; i--)
            {
                if ((int)engine_font.MeasureString(full_text.Substring(0, i)).X <= length || i == 1) // calculate cutoff point or split at the very first character (counteracted by padding value)
                {
                    result[0] = new text_element(t.get_font(), t.get_color(), full_text.Substring(0, i));
                    result[1] = new text_element(t.get_font(), t.get_color(), full_text.Substring(i));
                    break;
                }
            }

            return result;
        }
    }
/*-------------------------------------------------------------------------------------------------------------------------------------------*/
//    END
/*-------------------------------------------------------------------------------------------------------------------------------------------*/
    /* Dictionary used to store - string key with a message element value. Store this list in a main game Engine - load from xml.
    Dictionary<string, message_element> game_message_dictionary = new Dictionary<string, message_element>();
    Dictionary<string, string> game_message_dictionary = new Dictionary<string, string>(); // IF decoder function is made to run every frame - then messages can be stored as encoded strings
    */


    /*
    When Animated Texture, Particle System and Text chat systems are completed
    A demo should contain
    -- basic weather effects,
    -- system chat window that shows editor messages in multi color and using variables,
    -- demo particle effect with collision and animatedtexture source.

    Next:
    -- user text input + typed commands and display of command and error/success message in system chat.
    -- do updates to lights, water so they are fully integrated with in-game editor
    -- make a loading circle at the start of game + developer name + test game name + build number on a black background with centered. transition to the demo after any button is pressed
    -- create a simplified character and let it explore the world freely, jump, fall and platform
    -- make adjustments to camera so it chases the character and is not alway firmly centered on it. Camera in editor will be moved by awsd as always, when editor is closed, camera will center on character again
    */
}
/* BACKUP of line converter
 public void convert_to_ordered_lines(List<List<text_element>> converted, int max_length)
        {
            List<List<text_element>> message = new List<List<text_element>>(); // create a temporary message/line structure - holds a whole message separated into lines.  
            List<text_element> line = new List<text_element>();                // create a temporary line    
            int current_length = 0;

            foreach (message_element m in messages)
            {
                foreach (text_element t in m.get_message())
                {
                    t.set_text(t.get_text() + " "); // add a space to separate words when rendered
                    Vector2 width = engine_font.MeasureString(t.get_text());

                    line.Add(t);
                    current_length += (int)width.X;

                    // decide if a line break happened
                    if (current_length >= max_length) // create a line break
                    {
                        message.Add(line);         // add line to temporary list of lines
                        line = new List<text_element>();   // create a new line
                        current_length = 0;
                    }
                }
                // create a line break when a message element ends as well to separate each message in queue properly
                message.Add(line);         // add line to temporary list of lines
                line = new List<text_element>();   // create a new line
                current_length = 0;
            }
            // when a message is completed - add current non completed line if it exists, then reverse the order and append to main list using AddRange
            if (line.Count > 0)
                message.Add(line);

            message.Reverse();
            converted.AddRange(message);
        }     
 */