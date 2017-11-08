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

    /// <summary>
    /// user interface actions assigned to buttons or other interactive elements
    /// </summary>
    public enum actions
    {
        // editor modes
        editor_mode_switch_add = 0,
        editor_mode_switch_delete,
        editor_mode_switch_select,
        editor_mode_switch_lights,
        editor_mode_switch_water,
        editor_mode_switch_tree,
        editor_submode_switch_radius,
        editor_submode_switch_line,
        editor_submode_switch_square,
        editor_submode_switch_hollow_square,
        game_exit,
        overall_context,
        toggle_subcontext,
        hide_menu,
        clear_all,
        fill_all,
        // interface presentation sliders
        update_slider_grid_transparency,    // editor grid
        update_slider_grid_color_red,
        update_slider_grid_color_green,
        update_slider_grid_color_blue,
        update_selection_transparency,      // selection matrix
        update_selection_color_red,
        update_selection_color_green,
        update_selection_color_blue,
        update_interface_transparency,      // User Interface
        update_interface_color_red,
        update_interface_color_green,
        update_interface_color_blue,
        hide_this_container,
        none,                               // this action will have nothing assigned to it but it will not be null
        update_brush_size,
        current_container_random_sliders,   // sets ALL sliders in current context randomly
        option_value_switch,                // for value trackers - switch value
        change_cell_design,                 // switch current active editor get_cell_address
        color_preview,                      // action that doesn't change anything - simply identifies element
        focus_input,                        // for text input - will make current hovered text_input element receive input until cancellation
        switch_theme,
        unlock_ui_move,                     // unlocks specific ui movement mode (reposition ui elements on screen)
        resolution_fullscreen,              // changes screen resolution to fullscreen mode
        resolution_fullscreen_reverse,
        resolution_1920_1080,
        resolution_1440_900,
        resolution_1366_768,
        resolution_1280_800,
        resolution_1024_576,
        go_to_world_origin,
        toggle_system_chat,
        // light color controls
        update_slider_light_color_red,      // update lights color red component
        update_slider_light_color_green,
        update_slider_light_color_blue,
        update_slider_light_intensity,
        update_slider_light_range,
        // particle demo
        enable_particle_test,               // generated inside editor - system particle test menu item
        change_test_particle_star,
        change_test_particle_square, 
        change_test_particle_circle, 
        change_test_particle_hollow_square, 
        change_test_particle_raindrop,
        change_test_particle_triangle, 
        change_test_particle_x,
        change_test_trajectory_fall,
        change_test_trajectory_chaos,
        change_test_trajectory_rise,
        change_test_trajectory_ballistic_curve,
        change_test_trajectory_static,
        change_test_trajectory_laser,
        change_test_particle_colorR,
        change_test_particle_colorG,
        change_test_particle_colorB,
        enable_particle_color_interpolation,
        change_test_particle_lifetime,
        change_test_particle_emitter_radius,
        change_test_particle_burst,
        change_test_particle_scale,
        change_test_particle_rotation,
        change_test_particle_creation_rate
    };

    /// <summary>
    ///  command passed by keyboard/mouse
    /// </summary>
    public enum command
    {
        left_click,
        left_hold,
        left_release,
        middle_hold,
        right_click,
        right_hold,
        right_release,
        alt_c,
        alt_e,
        alt_q,
        alt_1,
        alt_2,
        alt_3,
        alt_4,
        mouse_scroll_up,
        mouse_scroll_down,
        enter_key,
        delete_key,
        insert_key,
        destroy_water_gen,
        destroy_lights,
        destroy_trees,
        destroy_grass,
        ctrl_plus_click
    };
  
    /// <summary>
    /// current_state = current current_state of GUI unit. default = inactive, hovered = mouse on top, highlighted = associated current_state active
    /// </summary>
    public enum state
    {
        default_state = 0,
        hovered = 1
    };
    /// <summary>
    ///  confirmation - UIunit action taken with or without additional dialog box
    /// </summary>
    public enum confirm
    {
        yes,
        no
    }

    /// <summary>
    /// modes = editor modes, ading/deleting/selcting cells
    /// </summary>
    public enum modes
    {
        add = 0,
        delete,
        select,
        prop_lights,
        water,
        prop_trees
    };
    /// <summary>
    /// submode tools = shape of added or deleted cells 
    /// </summary>
    public enum tools
    {
        radius = 0,
        line,           // line tool - special case of square tool
        square,
        hollow_square,
    };

    /// <summary>
    /// type of the UI element created
    /// </summary>
    public enum type
    {
        button,
        slider,
        value_button_binarychoice, // clicking on this button will change value tracked by it, updated value will be reassigned where it belongs by update function in the host class (where GUI object exists) 
        expandable_button,
        info_label,                // only text is displayed, no action taken on click
        id_button,                 // tracks a value
        vertical_scrollbar,
        color_preview,
        progress_bar,
        progress_circle,
        text_input,
        text_area,
        locker
    };
    /// <summary>
    /// Context type of the container
    /// </summary>
    public enum context_type
    {
        none,
        context,
        expansion
    }

    /// <summary>
    /// sector content = what contexttype of component exists in the Unit zone
    /// </summary>
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
 
    /// <summary>
    /// Game screens
    /// </summary>
    public enum screens
    {
        screen_main,
        screen_menu,
        screen_statistics
    };
    /// <summary>
    /// Sinewave starting position. 0 or 1
    /// </summary>
    public enum sinewave
    {
        zero,
        one
    }
    /// <summary>
    /// Water flow direction
    /// </summary>
    public enum waterflow
    {
        down,
        side
    }
}

