# Configure project settings

> [!NOTE]
> To use Visual Scripting in a project for the first time, you must [initialize it](#Initialize) from the Editor's [Project Settings](https://docs.unity3d.com/Manual/comp-ManagerGroup.html) window. 

Use the Project Settings window with Visual Scripting to manage backups, node assemblies, type options, and regenerate your Node Library.

To open your Project Settings: 

1. [!include[vs-open-project-settings](./snippets/vs-open-project-settings.md)]
1. Select **Visual Scripting**.  

You can find the following configuration options in your Visual Scripting Project Settings. To use Visual Scripting in a project for the first time, you must [regenerate your Node Library](#Regen), as described in the table below. 

| Option | Description |
|---|---|
| Initialize Visual Scripting | You must select **Initialize Visual Scripting** the first time you use Visual Scripting in a project. Initialize Visual Scripting to parse all assemblies and types for the Visual Scripting Node Library. After you initialize Visual Scripting, regenerate your Node Library. Refer to **<a href="#Regen">Regenerate Nodes</a>**, below. |
| Type Options | Use the Type Options list to add or remove types for your node inputs and outputs. After you add or remove a type, you must regenerate your Node Library. Refer to **<a href="#Regen">Regenerate Nodes</a>**, below. <br/>For more information on how to add or remove types, refer to <a href="vs-add-remove-type-options.md">Add or remove types</a>. |
| Node Library | Use the Node Library list to add or remove nodes and their assemblies in Visual Scripting. You must add any new types to your Type Options after you add new nodes to Visual Scripting. You must also regenerate your Node Library after you add or remove nodes. Refer to <a href="#Regen">**Regenerate Nodes**</a>, below. <br/>For more information on how to add or remove nodes from your Node Library, refer to <a href="vs-add-remove-node-library.md">Add or remove available nodes</a>. |
| Regenerate Nodes | Regenerate your Node Library to make all nodes available for use in a project. <br/>To use Visual Scripting for the first time in a project, you must **<a href="#Initialize">Initialize Visual Scripting</a>** and regenerate your Node Library. <br/>To regenerate your Node Library: <br/> 1. Select **Regenerate Nodes**. <br/> 2. Select **OK**.<br/> You must regenerate your Node Library in the following circumstances: <br/> * Before you use Visual Scripting in your project for the first time. <br/> * After you add or remove nodes from your Node Library. <br/> * After you add or remove types from your Type Options. <br/> * After you change the inputs or outputs for a Custom C# node. |
| Generate | To generate required property provider scripts for custom drawers, select **Generate**. <br/>These scripts are necessary for Unity to use custom drawers for custom classes and script variables inside Visual Scripting. To assign a default value to a custom variable type through the Unity Editor’s Inspector, you must either have access to the source code for the class, or provide a custom PropertyDrawer. For more information, see <a href="vs-custom-types.md">Custom types</a>. |
| Create Backup | To create a new backup of your Visual Scripting graphs and settings, select **Create Backup**. <br/> For more information about backups, refer to <a href="vs-create-restore-backups.md">Create or restore a backup</a>. | 
| Restore Backup | To open the folder where Visual Scripting stores your backups, select **Restore Backup**. <br/>For more information about backups, refer to <a href="vs-create-restore-backups.md">Create or restore a backup</a>. |
| Fix Missing Scripts | To correct any issues that might occur after migration from the Unity Asset Store version of Visual Scripting to the package version, select **Fix Missing Scripts**. This resolves any missing references to Visual Scripting Script Graphs and State Graphs in Script Machine or State Machine components. |

>[!NOTE] 
> If your settings don't apply after you make a change, [report a bug through the Unity Editor](https://unity3d.com/unity/qa/bug-reporting).
