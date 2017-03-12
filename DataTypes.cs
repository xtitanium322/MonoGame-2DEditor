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
 * XML world Tile data loaded into this
 * IMPORTANT - always define all variables public in these datatypes
 */
namespace MyDataTypes
{
    public class MapData
    {
        public string block_name;  // name of the Tile to be drawn
        public int block_x;        // position horizontal, starting x coordinate 
        public int block_y;        // position vertical,   starting y copordinate
        public int width;          // how many Tile to the right 
        public int height;         // how many ui_elements up
    }

    // Need to create a loading function that will parse data contained in xml and do the following:
    /// <summary>
    /// 1. create a Container
    /// 2. create stored_elements for this container
    /// 3. add this unit to container
    /// 4. add container to GUI
    /// 5. need to assign subcontext containers to a expandable button Unit
    /// </summary>
    public class ContainerData // load GUI containers from xml
    {
        public string c_id;
        public string contexttype; // parse to pre_alpha.context_type
        public string name;
        public int origin_x;
        public int origin_y;
        public string visible;
    }

    public class ElementData // load GUI elements from xml,
    {
        public string id;
        public string parent_id;
        public string subcontext_id; // -1 = no subcontext, otherwise container id here
        public string ui_type;//pre_alpha.contexttype
        public string action;//pre_alpha.actions?
        public string confirmation;//pre_alpha.confirm
        public int dimension_x; // for Rectangle 
        public int dimension_y;
        public int dimension_w;
        public int dimension_h;
        public string icon_name;
        public string label;
        public string tooltip;
    }

    public class UIData // hierarchy of GUI elements
    {
        public string assignment_type; //1-element to container(el_to_con), 2-container to GUI(con_to_ui) or 3- subContainer to element(sub_to_el)
        public int target_id;       // target for asssignment, target id -1 = GUI
        public int component_id;    // being assigned
    }

    /*
 <?xml version="1.0" encoding="utf-8"?>
<XnaContent>
  <Asset Type="MyDataTypes.ContainerData[]">
    <Item>
      <contexttype>none</contexttype>
      <name>mode</name>
      <origin>300,15</origin>
      <visible>true</visible>
    </Item>
  </Asset>
   <Asset Type="MyDataTypes.ContainerData[]">
   </Asset>
</XnaContent>
 */

    /// example format for file
    /*<?xml version="1.0" encoding="utf-8"?>
    <XnaContent>
      <Asset Type="MyDataTypes.MapData[]">
        <Item>
          <block_name>dirt</block_name>
          <block_x>1</block_x>
          <block_y>1</block_y>
          <width>1</width>
          <height>1</height>
        </Item>
       </Asset>
    </XnaContent>
     */
}
