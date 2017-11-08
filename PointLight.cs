using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace EditorEngine
{
    /// <summary>
    /// Point Light - used as a light aura around objects or a self-sufficient light source in the world.
    /// </summary>
    public class PointLight
    {
        public Color light_color;      // rgba
        public Vector2 position;       // in relation to map origin
        public Vector2 cell;           // cell address of this light
        public float sphere_radius;    // r of the sphere which this light covers
        public float light_intensity;  // how bright is this light?
        public bool removable;         // true - can be deleted, false - not deleted
        public bool spriteless;        // true - no sprite will be used
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="e">engine instance</param>
        /// <param name="cell">cell address</param>
        /// <param name="color">light color</param>
        /// <param name="pos">pixel position</param>
        /// <param name="radius">radius of the effect</param>
        /// <param name="intensity">light intensity - brightness</param>
        public PointLight(Engine e, Vector2 cell, Color color, Vector2 pos, float radius, float intensity)
        {
            this.cell = cell;
            light_color = color;
            position = pos;
            sphere_radius = radius;
            light_intensity = intensity;
        }
        /// <summary>
        /// Update light color value
        /// </summary>
        /// <param name="c"></param>
        public void change_light_color(Color c)
        {
            light_color = c;
        }
        /// <summary>
        /// Update light intensity
        /// </summary>
        /// <param name="val">float value</param>
        public void change_intensity(float val)
        {
            light_intensity = val;
        }
        /// <summary>
        /// Update the range of effect
        /// </summary>
        /// <param name="val">int value in pixels - radius of the circle</param>
        public void change_range(int val)
        {
            sphere_radius = val;
        }
        /// <summary>
        /// Active radius represents the pulsing of the light
        /// </summary>
        /// <returns>active radius in pixels</returns>
        public float active_radius()
        {
            return sphere_radius;
        }
        /// <summary>
        /// Get the current light color
        /// </summary>
        /// <returns>Color object</returns>
        public Color get_color()
        {
            return light_color;
        }
        /// <summary>
        /// Get active radius of effect
        /// </summary>
        /// <returns>value in pixels</returns>
        public float get_radius()
        {
            return sphere_radius;
        }
        /// <summary>
        /// get light intenisty 
        /// </summary>
        /// <returns>float value. 1f represents 100%</returns>
        public float get_intensity()
        {
            return light_intensity;
        }
    }
}