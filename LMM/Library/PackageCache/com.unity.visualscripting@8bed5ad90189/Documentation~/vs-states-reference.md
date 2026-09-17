# State Graph nodes

Visual scripting has four nodes that you can use to identify and manipulate the State Graphs assigned to a GameObject and its State Machines: 

- [Set State Graph](#SetState)
- [Has State Graph](#HasState) 
- [Get State Graph](#GetState) 
- [Get State Graphs](#GetStates)

## <a name="SetState">Set State Graph</a> 

You can use the Set State Graph node to assign a State Graph to a specific State Machine, or to the first State Machine attached to a specific GameObject. 

![The Set State Graph node](images/VS-set-state-graph-node.png)

| **Label** | **Item** | **Description** |
| :--- | :--- | :--- |
| ![The letter A in a blue circle, matching the label on the Set State Graph node image.](images/Label-A.png) | Node Parameters > **Container Type** | Specifies whether the Target is a GameObject or State Machine. |
| ![The letter B in a blue circle, matching the label on the Set State Graph node image.](images/Label-B.png) | Input Ports > **Enter** | (Input Trigger) The execution input trigger for the node. |
| ![The letter C in a blue circle, matching the label on the Set State Graph node image.](images/Label-C.png) | Input Ports > **Target** | (GameObject or State Machine) The GameObject or State Machine where the node sets the Graph. |
| ![The letter D in a blue circle, matching the label on the Set State Graph node image.](images/Label-D.png) | Input Ports > **Graph** | (State Graph Asset) The State Graph the node sets on the Target. |
| ![The letter E in a blue circle, matching the label on the Set State Graph node image.](images/Label-E.png) | Output Ports > **Exit** | (Output Trigger) The execution output trigger. |
| ![The letter F in a blue circle, matching the label on the Set State Graph node image.](images/Label-F.png) | Output Ports > **Graph** | (State Graph Asset; Optional) Outputs the Graph. |



### Setting the required node parameters and inputs

The Set State Graph node has one required input parameter, called **Container Type**, which is set using the dropdown in the node's header. The **Container Type** specifies what component type the node should expect as an input for its **Target**:

- If you choose **GameObject**, the node expects to receive a GameObject, and assigns the graph to the first State Machine attached to that GameObject.

- If you choose **State Machine**, the node expects to receive a State Machine, and you can specify the exact State Machine where you want to set your State Graph. 

Depending on which **Container Type** you select, the icon displayed next to the **Target** input port on the node changes: 

| **Container Type** | **Target Icon** |
| :--- | :--- |
| **GameObject** | ![The GameObject icon](images/vs-gameobject-icon.png)|
| **State Machine** | ![The State Machine icon](images/vs-state-machine-icon.png)|

The node has three input ports, located on the left side. The first port, **Enter**, connects to the node that should start the execution of the Set State Graph node.

The other two ports collect the Set State Graph node's required input data: 

- The **Target**, or the GameObject or State Machine where you want to set a State Graph.
 
- The **Graph**, or the State Graph to assign to the GameObject or State Machine.

### Outputs

The Set State Graph node has two output ports, located on the right side. 

The first port, **Exit**, establishes the connection to the node that should execute after the Set State Graph node has finished. 

The second port, **Graph**, can output the State Graph that you assigned using the node. 

## <a name="HasState">Has State Graph</a>

The Has State Graph node allows you to determine whether a GameObject or State Machine has a specific State Graph assigned to it. 

![The Has State Graph node](images/VS-has-state-graph-node.png)

|**Label** | **Item** | **Description** |
| :--- | :--- | :--- |
| ![The letter A in a blue circle, matching the label on the Has State Graph node image.](images/Label-A.png) | Node Parameters > **Container Type** | Specify whether the Target is a GameObject or State Machine. |
| ![The letter B in a blue circle, matching the label on the Has State Graph node image.](images/Label-B.png) | Input Ports > **Enter** | (Input Trigger) The execution input trigger for the node. |
| ![The letter C in a blue circle, matching the label on the Has State Graph node image.](images/Label-C.png) | Input Ports > **Target** | (GameObject or State Machine) The GameObject or State Machine where the node should check for the Graph. |
| ![The letter D in a blue circle, matching the label on the Has State Graph node image.](images/Label-D.png) | Input Ports > **Graph** | (State Graph Asset) The State Graph to search for on the GameObject or State Machine. |
| ![The letter E in a blue circle, matching the label on the Has State Graph node image.](images/Label-E.png) | Output Ports > **Exit** | (Output Trigger) The execution output trigger, which starts execution of the next node in the flow after checking for the specified State Graph. |
| ![The letter F in a blue circle, matching the label on the Has State Graph node image.](images/Label-F.png) | Output Ports > **Has Graph** | (Boolean) Outputs `true` if the node found the specified State Graph, `false` if not. |

### Setting the required node parameters and inputs

The Has State Graph node has one required input parameter, called **Container Type**, which is set using the dropdown in the node's header. The **Container Type** specifies what component type the node should expect as an input for its **Target**:

- If you choose **GameObject**, the node expects to receive a GameObject, and checks for the graph on the first State Machine attached to that GameObject.

- If you choose **State Machine**, the node expects to receive a State Machine, and you can specify the exact State Machine where you want to check for the State Graph. 

Depending on which **Container Type** you select, the icon displayed next to the **Target** input port on the node changes: 

| **Container Type** | **Target Icon** |
| :--- | :--- |
| **GameObject** | ![The GameObject icon](images/vs-gameobject-icon.png)|
| **State Machine** | ![The State Machine icon](images/vs-state-machine-icon.png)|

The node has three input ports, located on the left side. The first port, **Enter**, connects to the node that should start the execution of the Has State Graph node.

The other two ports collect the Has State Graph node's required input data: 

- The **Target**, or the GameObject or State Machine where you want to check for a State Graph.
 
- The **Graph**, or the State Graph to search for on the GameObject or State Machine.

### Outputs

The Has State Graph node returns `true` if it finds the specified State Graph. Otherwise, it returns `false`. 

You can use a control node connected to the Has State Graph's output port to change what your script does next, based on the result from Has State Graph. For more information about control nodes, see [Control nodes](vs-control.md). 

## <a name="GetState">Get State Graph</a>

The Get State Graph node returns the first State Graph set on a GameObject. 

![The Get State Graph node](images/VS-get-state-graph-node.png)

| **Label** | **Item** | **Description** |
| :--- | :--- | :--- |
| ![The letter A in a blue circle, matching the label on the Get State Graph node image.](images/Label-A.png) | Input Ports > **GameObject** | (GameObject) The GameObject where the node should retrieve a set State Graph. |
| ![The letter B in a blue circle, matching the label on the Get State Graph node image.](images/Label-B.png) | Output Ports > **Graph** | (State Graph Asset) Outputs the first or only State Graph set on the GameObject, or `null` if there is no set State Graph. |


### Setting the required node parameters and inputs

The Get State Graph node is a data node. It can't control any logic in your script, and is only used to return data. 

The node has a single input port, located on the left side, which collects the node's required input data: 

- The GameObject where the node should retrieve the State Graph. You can choose a specific GameObject, or leave the default selection as **This** to use the GameObject where your script is currently running. 


### Outputs 

The Get State Graph node has a single output port, located on the right side. The output port returns the GameObject's first set State Graph, or `null`, if there is no set State Graph. 

> [!NOTE]
> The Get State Graph node returns only the first State Graph set on a GameObject. To return all State Graphs set on a GameObject, use the [Get State Graphs node](#GetStates). 

## <a name="GetStates">Get State Graphs</a>

The Get State Graphs node returns a list of all State Graphs set on a GameObject. 

![The Get State Graphs node](images/VS-get-state-graphs-node.png)

| **Label** | **Item** | **Description** |
| :--- | :--- | :--- |
| ![The letter A in a blue circle, matching the label on the Get State Graphs node image.](images/Label-A.png) | Input Ports > **GameObject** | (GameObject) The GameObject where the node should retrieve a list of set State Graphs. |
| ![The letter B in a blue circle, matching the label on the Get State Graphs node image.](images/Label-B.png) | Output Ports > **Graphs** | (List of State Graph Assets) Outputs a list of all State Graphs set on the GameObject, or an empty list if there are no set State Graphs. |


### Setting the required node parameters and inputs

The Get State Graphs node is a data node. It can't control any logic in your script, and is only used to return data. 

The node has a single input port, located on the left side, which collects the node's required input data: 

- The GameObject where the node should retrieve a list of State Graphs. You can choose a specific GameObject, or leave the default selection as **This** to use the GameObject where your script is currently running. 

### Outputs 

The Get State Graphs node has a single output port, located on the right side. The output port returns a list of all set State Graphs for the GameObject, or an empty list, if there are no set State Graphs. 
