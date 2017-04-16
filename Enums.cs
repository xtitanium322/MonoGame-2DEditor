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

namespace beta_windows
{
    /// <summary>
    /// All the enumerations used inside Game classes
    /// </summary>
    // text label orientation inside it'engine element
    public enum orientation
    {
        horizontal,
        vertical,
        both,
        vertical_left,
        vertical_right,
        horizontal_bottom
    };
    // user interface actions
    public enum actions
    {
        editor_mode_switch_add = 0,
        editor_mode_switch_delete,
        editor_mode_switch_select,
        editor_mode_switch_lights,
        //editor_submode_switch, 
        game_exit,
        overall_context,
        toggle_subcontext,
        hide_menu,
        clear_all,
        fill_all,
        update_slider_grid_transparency, // editor grid
        update_slider_grid_color_red,
        update_slider_grid_color_green,
        update_slider_grid_color_blue,
        update_selection_transparency,   // selection matrix
        update_selection_color_red,
        update_selection_color_green,
        update_selection_color_blue,
        update_interface_transparency,   // User Interface
        update_interface_color_red,
        update_interface_color_green,
        update_interface_color_blue,
        hide_this_container,
        none, // this action will have nothing assigned to it but it will not be null
        update_brush_size,
        current_container_random_sliders, // sets ALL sliders in current context randomly
        option_value_switch, // for value trackers - switch value
        change_cell_design, // switch current active editor cell
        color_preview, // action that doesn't change anything - simply identifies element
        focus_input, // for text input - will make current hovered text_input element receive input until cancellation
        switch_theme,
        unlock_ui_move, // unlocks specific ui movement mode (reposition ui elements on screen)
        resolution_fullscreen, // changes screen resolution to fullscreen mode
        resolution_fullscreen_reverse,
        resolution_1920_1080,
        resolution_1440_900,
        resolution_1366_768,
        resolution_1280_800,
        resolution_1024_576,
        go_to_world_origin

    };
    // command passed by keyboard/mouse
    public enum command
    {
        left_click,
        left_hold,
        left_release,
        right_click,
        right_hold,
        right_release,
        alt_e,
        alt_q,
        alt_1,
        alt_2,
        alt_3,
        alt_4,
        mouse_scroll_up,
        mouse_scroll_down,
        enter_key
    };
    // current_state = current current_state of GUI unit. default = inactive, hovered = mouse on top, highlighted = associated current_state active
    public enum state
    {
        default_state = 0,
        hovered = 1
    };
    // confirmation - UIunit action taken with or without additional dialog box
    public enum confirm
    {
        yes,
        no
    }
    // modes = editor modes, ading/deleting/selcting cells
    public enum modes
    {
        add = 0,
        delete,
        select,
        prop_lights,
        prop_trees
    };
    // submode = shape of added or deleted cells 
    public enum tools
    {
        radius = 0,
        line, // line tool - special case of square tool
        square,
        hollow_square,
        water // adds water ui_elements with full volume
    };
    // contexttype = what contexttype of GUI unit this is - might be redundant with creation of sector_content functionality
    public enum type
    {
        button,
        slider,
        value_button_binarychoice, // clicking on this button will change value tracked by it, updated value will be reassigned where it belongs by update function in the host class (where GUI object exists) 
        expandable_button,
        info_label, // only text is displayed, no action taken on click
        id_button,   // tracks a value
        vertical_scrollbar,
        color_preview,
        progress_bar,
        progress_circle,
        text_input,
        text_area,
        locker
    };
    public enum context_type
    {
        none,
        context,
        expansion
    }
    // sector content = what contexttype of component exists in the Unit zone
    public enum sector_content
    {
        icon = 0,
        label,
        min_slider,
        max_slider,
        slider_area,
        current_slider_value,
        yn_option,
        multioption,
        active_indicator,
        expansion_indicator,
        vertical_slider,
        color_preview,
        progress,
        progress_label,
        circular_progress,
        text_input_display,
        text_area,
        locker_state
    };
    // screens
    public enum screens
    {
        screen_main,
        screen_menu,
        screen_statistics
    };
    public enum sinewave
    {
        zero,
        one // beginning points
    }
    public enum waterflow
    {
        down,
        side
    }
}

