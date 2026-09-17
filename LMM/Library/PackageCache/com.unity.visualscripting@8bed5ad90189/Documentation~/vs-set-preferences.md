# Configure your preferences

You can configure specific preferences in Visual Scripting to control the behavior of the [Graph window](vs-interface-overview.md) and your nodes. 

To configure your preferences for Visual Scripting: 

1. Go to **Edit** > **Preferences**. 
1. Select **Visual Scripting**. 

## Core preferences

The following preferences control general behaviors across all graph types in Visual Scripting. 

| **Preference** | **Description** |
|:---|:---|
| **Dim Inactive Nodes** | Enable to dim any nodes in the [Graph Editor](vs-interface-overview.md#the-graph-editor) that aren't connected to the logic flow in a graph. This provides you with a visual cue that a dimmed node isn't used in the graph in its current configuration. Disable to display all nodes as active, regardless of their connection state. Note: You can also control this preference from the Graph toolbar. For more information, see [The interface](vs-interface-overview.md#the-graph-toolbar). |
| **Dim Incompatible Nodes** | Enable to dim all nodes that don't have a compatible connection port when you create a new edge. Disable to display all nodes as active for a new edge. |
| **Show Variables Help** | Enable to display a brief explanation of the selected variable scope in the [Blackboard](vs-interface-overview.md#the-blackboard). Disable to hide these explanations. |
| **Create Scene Variables** | Enable to automatically create a **Scene Variables** GameObject with a **Variables** component and a Scene Variables script component after you create a [Scene variable](vs-variables.md#variable-scopes). A GameObject with these components is required to use Scene variables in a project. Disable to create these components on a GameObject manually. |
| **Show Grid** | Enable to display a grid on the background of the [Graph Editor](vs-interface-overview.md#the-graph-editor). Disable to hide the grid. |
| **Snap to Grid** | Enable to force nodes to stick or snap to points on a grid in the [Graph Editor](vs-interface-overview.md#the-graph-editor). Disable to move nodes freely and disable the snap-to-point behavior. |
| **Pan Speed** | Set a value to control how quickly the view in the [Graph Editor](vs-interface-overview.md#the-graph-editor) moves when you pan vertically with the scroll wheel. |
| **Drag Pan Speed** | Set a value to control how quickly the view in the [Graph Editor](vs-interface-overview.md#the-graph-editor) moves when you move a node to the edge of the Graph window. |
| **Zoom Speed** | Set a value to control how quickly the [Graph Editor](vs-interface-overview.md#the-graph-editor) zooms in or zooms out while you change the zoom level in the Graph window. For more information on how to change the zoom level in the Graph Editor, see [Choose a control scheme](vs-control-schemes.md#zoom-inzoom-out). |
| **Overview Smoothing** | Set a value to control how gradually the [Graph Editor](vs-interface-overview.md#the-graph-editor) zooms or pans after you select the **Overview** option in the [Graph toolbar](vs-interface-overview.md#the-graph-toolbar). |
| **Carry Children** | Enable to move all connected child nodes when you move a parent node in the [Graph Editor](vs-interface-overview.md#the-graph-editor). Disable to only move the currently selected node in the Graph Editor. Note: You can also change this setting from the Graph toolbar in the Graph window. For more information, refer to [The interface](vs-interface-overview.md#the-graph-toolbar). |
| **Disable Playmode Tint** | Enable to display all nodes in the Graph window as normal while the Unity Editor is in Play mode. Disable to add a tint to all nodes in the Graph window while the Editor is in Play mode. For more information on Play mode, see [The Game view](https://docs.unity3d.com/Manual/GameView.html) in the Unity User Manual. | 
| **Control Scheme** | Select a Visual Scripting control scheme. For more information, refer to [Choose a control scheme](vs-control-schemes.md). <ul><li>**Default**: Use the Default Visual Scripting control scheme.</li><li>**Alternate**: Use the Alternate Visual Scripting control scheme.</li></ul> |
| **Clear Graph Selection** | Enable to clear any graph displayed in the Graph window after you select a GameObject with no set graph or graphs. Disable to keep the last displayed graph if the selected GameObject has no set graph assets. Note: Visual Scripting always updates the Graph window to display the set graph on a selected GameObject, regardless of your chosen Clear Graph Selection setting. |
| **Human Naming** | Enable to convert all displayed method names from camel case to title case. For example, `camelCase` becomes `Camel Case`. Disable to leave all names in camel case. |
| **Max Search Results** | Set a value to specify the maximum number of search results returned by [the fuzzy finder](vs-interface-overview.md#the-fuzzy-finder) after you use the search bar. |
| **Group Inherited Members** | Enable to group together inherited nodes from a parent or base class to your current search term in [the fuzzy finder](vs-interface-overview
| **Developer Mode** | Enable to display additional preferences in the Preferences window and add additional features in the Graph window and other areas of the Unity Editor. For more information on the additional Developer Mode preferences, refer to [Additional Developer Mode preferences](#additional-developer-mode-preferences). |
| **AOT Safe Mode** | Enable to exclude nodes from search results in [the fuzzy finder](vs-interface-overview.md#the-fuzzy-finder) that might cause problems for platforms that require ahead of time (AOT) compilation. For example, Visual Scripting excludes nodes that use the `Generic` type. Disable to display all nodes and types in the fuzzy finder. |


## Script Graphs preferences 

The following preferences change the behavior of Script Graphs in the Graph window.

| **Preference** | **Description** |
| :--- | :--- |
| **Update Nodes Automatically** |<div class="NOTE"><h5>NOTE</h5><p>This feature is experimental.</p></div>Enable **Update Nodes Automatically** to let Visual Scripting automatically update your Node Library when it detects a change in any script inside your project's **Assets** folder. <br/>Disable **Update Nodes Automatically** to manually regenerate your Node Library after you make a change to a script. For more information on how to regenerate your Node Library, refer to <a href="vs-configuration.md">Configure project settings</a>.|
| **Predict Potential Null References** | A predictive debugging feature. Enable **Predict Potential Null References** to display warnings about potential `null` value inputs in your graphs. <br/>Disable **Predict Potential Null References** to disable these warnings. <br/> <div class="NOTE"><h5>NOTE</h5><p>Sometimes, predictive debugging might return false positive results when you enable this setting.</p></div> |
| **Predict Potential Missing Components** | A predictive debugging feature. Enable **Predict Potential Missing Components** to display warnings about potential missing components in your graphs, such as a missing node input. <br/>Disable **Predict Potential Missing Components** to disable these warnings. <br/> <div class="NOTE"><h5>NOTE</h5><p>Sometimes, predictive debugging might return false positive results when you enable this setting.</p></div> |
| **Show Connection Values** | Enable **Show Connection Values** to display the input and output values sent between nodes while the Editor is in Play mode. This can make it easier to debug your scripts. <br/>Disable **Show Connection Values** to hide these value labels while in Play mode. For more information on Play mode, refer to <a href="https://docs.unity3d.com/Manual/GameView.html">The Game view</a> in the User Manual. <div class="NOTE"><h5>NOTE</h5><p>You can also control this preference from the Graph toolbar. For more information, refer to <a href="vs-interface-overview.md#the-graph-toolbar">The interface</a>.</p></div> |
| **Predict Connection Values** | Enable **Predict Connection Values** to have the Graph Editor predict what input and output values your graph sends between nodes while the Unity Editor is in Play mode. For example, Visual Scripting would display the value currently set for a variable in your script, though that value might change before it's used by a node. <br/>Disable **Predict Connection Values** to hide these predicted input and output values. |
| **Hide Port Labels** | Enable **Hide Port Labels** to hide the name labels for node input and output ports. <br/>Disable **Hide Port Labels** to display these name labels. |
| **Animate Control Connections** | Enable **Animate Control Connections** to display a droplet animation across node control port edges while the Editor is in Play mode. <br/>Disable **Animate Control Connections** to disable the animations. For more information about the different node port types and edges, refer to <a href="vs-nodes.md">Nodes</a>. For more information on Play mode, refer to <a href="https://docs.unity3d.com/Manual/GameView.html">The Game view</a> in the User Manual. |
| **Animate Value Connections** | Enable **Animate Value Connections** to display a droplet animation across node data port edges while the Editor is in Play mode. <br/>Disable **Animate Value Connections** to disable the animations. For more information about the different node port types and edges, refer to <a href="vs-nodes.md">Nodes</a>. For more information on Play mode, refer to <a href="https://docs.unity3d.com/Manual/GameView.html">The Game view</a> in the User Manual. | 
| **Skip Context Menu** | Enable **Skip Context Menu** to always open <a href="vs-interface-overview.md#the-fuzzy-finder">the fuzzy finder</a> when you right-click in the Graph Editor. To access the context menu, use Shift+right-click. <br/>Disable **Skip Context Menu** to open the fuzzy finder when you right-click with no nodes or groups selected in the Graph Editor. The context menu opens when you right-click with a node or group selected. |

## State Graphs preferences

The following preferences change the behavior of State Graphs in the Graph window. 


### State reveal

Use the dropdown to choose when a Script State node displays a list of events from its graph.

If you have many Script State nodes in a State Graph, you might want to change this setting.

| **Preference** | **Description** |
| :--- | :--- |
| **Never** | Script State nodes never display their list of events. |
| **Always** | Script State nodes always display their list of events. |
| **On Hover** | Script State nodes only display their list of events when you hover over the node in the Graph window. |
| **On Hover with Alt** | Script State nodes only display their list of events when you hover over the node while you hold Alt. |
| **When Selected** | Script State nodes only display their list of events when you select the node in the Graph window. |
| **On Hover or Selected** | Script State nodes display their list of events when you hover over the node, or when you select the node in the Graph window. |
| **On Hover with Alt or Selected** | Script State nodes display their list of events when you hover over the node while you hold Alt, or when you select the node in the Graph window. |

### Transitions Reveal

Use the dropdown to choose when a transition displays a list of events from its graph.

If you have many transitions in a State Graph, you might want to change this setting.

| **Preference** | **Description** |
| :--- | :--- |
| **Never** | Transitions never display a list of events. |
| **Always** | Transitions always display a list of events. |
| **On Hover** | Transitions only display a list of events when you hover over the transition in the Graph window. |
| **On Hover with Alt** | Transitions only display a list of events when you hover over the transition while you hold Alt. |
| **When Selected** | Transitions only display a list of events when you select the transition in the Graph window. |
| **On Hover or Selected** | Transitions display a list of events when you hover over the transition, or when you select the transition in the Graph window. |
| **On Hover with Alt or Selected** | Transitions display a list of events when you hover over the transition while you hold Alt, or when you select the transition in the Graph window. |

### Transitions End Arrow

Enable **Transitions End Arrow** to add an arrow to the end of each transition edge in a State Graph. Disable **Transitions End Arrow** to display edges between transitions as simple lines. 

If you have many transitions in your State Graphs, you might want to disable this setting.

### Animate Transitions

Enable **Animate Transitions** to display a droplet animation across transition edges when the Editor is in Play mode. Disable **Animate Transitions** to disable the animations. For more information on Play mode, refer to <a href="https://docs.unity3d.com/Manual/GameView.html">The Game view</a> in the User Manual.

## Additional Developer Mode preferences

> [!NOTE]
> You can only access the following preferences after you have enabled **Developer Mode* in your [Core preferences](#core-preferences).

These Developer Mode preferences provide help with developing extensions or custom nodes for Visual Scripting. Their continued support in the Visual Scripting package isn't guaranteed. 

|**Preference** |**Description** |
|:---|:---|
|**Debug** | Enable **Debug** to add additional logging and visual overlays to help you debug element rendering in the Graph window. For example, if you created a custom node, use this setting to help debug your UI. <br/>Disable **Debug** to disable the logging and hide these overlays.|
|**Track Metadata State** | Enable **Track Metadata State** to add more information to logging. This can assist in debugging. <br/>Disable **Track Metadata State** to hide this additional information.|
|**Debug Inspector UI** | Enable **Debug Inspector UI** to add more overlays and additional details. The information available is greater than what Visual Scripting provides with the **Debug** setting, and affects more areas of the Editor's UI. Only enable this setting if you need more in-depth debugging feedback. <br/>Disable **Debug Inspector UI** to hide this information.|
