using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// A class containing UI elements for dialogs to use.
/// </summary>
[System.Serializable]
public class DialogContext
{
    public Text dialogTextField, whoTextField;
    public Button[] choiceButtons;
}

/// <summary>
/// Base interface for all of the dialogs.
/// </summary>
public interface BaseDialog
{
    /// <summary>
    /// Called when the dialog is being activated i.e. the dialog started and it was the first dialog node or the dialog jumped to this node. Note: Deactivate every button you don't use.
    /// </summary>
    /// <param name="ctx">UI elements for the dialog to prepare.</param>
    void process(DialogContext ctx);

    /// <summary>
    /// Returns whether the player can jump to the next node.
    /// </summary>
    bool isNextNodeAvailable { get; }

    /// <summary>
    /// Returns the next node. Called when the isNextNodeAvailable is true and the player clicked a button or Return key (Enter).
    /// </summary>
    BaseDialog nextNode { get; }
}

/// <summary>
/// A dialog that just shows a dialog line.
/// </summary>
public class ShowDialog : BaseDialog
{
    public string dialog, who;

    public BaseDialog nextNode { get; set; }

    public bool isNextNodeAvailable => true;

    public void process(DialogContext ctx)
    {
        ctx.dialogTextField.text = dialog;
        ctx.whoTextField.text = who;
        for (int x = 0; x < ctx.choiceButtons.Length; ++x)
        {
            ctx.choiceButtons[x].gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// A dialog with multiple choices.
/// </summary>
public class ChoiceDialog : BaseDialog
{
    public string dialog, who;
    public string[] choices;
    public BaseDialog[] choiceNodes;
    private int chosen = -1;

    public BaseDialog nextNode
    {
        get
        {
            return chosen >= 0 ? choiceNodes[chosen] : null;
        }
    }

    public bool isNextNodeAvailable
    {
        get
        {
            return chosen >= 0;
        }
    }

    public void process(DialogContext ctx)
    {
        ctx.whoTextField.text = who;
        ctx.dialogTextField.text = dialog;
        int i = 0;
        // Here we prepare the buttons for our choices...
        for (; i < choices.Length; ++i)
        {
            ctx.choiceButtons[i].gameObject.SetActive(true);
            ctx.choiceButtons[i].GetComponentInChildren<Text>().text = choices[i];
            int currId = i;
            ctx.choiceButtons[i].onClick.AddListener(()=>{
                chosen = currId;
                DialogsDatabase.instance.processNextNode();
            });
        }
        // ...and here we deactivate the rest of them.
        for (; i < ctx.choiceButtons.Length; ++i)
        {
            ctx.choiceButtons[i].gameObject.SetActive(false);
        }
    }
}
