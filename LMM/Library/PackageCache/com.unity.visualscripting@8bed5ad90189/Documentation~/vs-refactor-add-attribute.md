# Add the RenamedFrom attribute to a C# script

To use nodes generated from a custom C# script in a project after you rename a member, class, struct, type, or enum, add the `[RenamedFrom]` attribute to the relevant API element in the script file. For more information on the `[RenamedFrom]` attribute, see [Refactor a C# script with Visual Scripting](vs-refactoring.md).

To add the attribute to a C# script:

1. [!include[vs-open-project-window](./snippets/vs-open-project-window.md)]
1. In the Project window, double-click the C# script file you want to refactor. Unity opens the file in the program you specified in your preferences, under **External Script Editor**.

    > [!NOTE]
    > For more information on script editors in Unity, refer to [Integrated development environment (IDE) support](https://docs.unity3d.com/Manual/ScriptingToolsIDEs.html) in the Unity User Manual.

1. In your external editor, do the following:

    1. Add the <code>[RenamedFrom]</code> attribute above the definition of the part of the script you want to rename.
    1. Add the element's old name as a string to the <code>[RenamedFrom]</code> attribute, as its parameter. For example:

    ```
    using UnityEngine; 
    using Unity.VisualScripting; 

    [RenamedFrom("Character")]
    public class Player : MonoBehaviour
    {
        [RenamedFrom("InflictDamage")]
        public void TakeDamage(int damage)
        {
            //...
        }
    }
    ```

1. [!include[vs-save-script](./snippets/vs-save-script.md)]
1. [!include[vs-return-unity](./snippets/vs-return-unity.md)]
1. [!include[vs-regen-node-library](./snippets/vs-regen-node-library.md)]

> [!NOTE]
> If you change the namespace or namespaces used in your script, you must include the old namespace or namespaces to use the `[RenamedFrom]` attribute. 

## Next steps

Unity recommends that you leave the attribute in the script file, even after a successful recompile. Nodes that use your C# script no longer have errors related to a missing member, class, struct, type, or enum.

## Additional resources

- [Refactor a C# script with Visual Scripting](vs-refactor-add-attribute.md)
- [Configure project settings](vs-configuration.md)
- [Add or remove types from your Type Options](vs-add-remove-type-options.md)
- [Custom C# nodes](vs-create-custom-node.md)
- [Custom events](vs-custom-events.md)