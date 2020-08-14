using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class DialogsDatabase : MonoBehaviour
{
    private Dictionary<string, string> dialogPairs;
    private Dictionary<string, BaseDialog> registeredDialogs;

    /// <summary>
    /// Specifies which files with dialog pairs to load during startup.
    /// </summary>
    public List<string> filesOfPairsToLoad;
    /// <summary>
    /// Specifies which files with dialog trees to load during startup.
    /// </summary>
    public List<string> filesOfDialogsToLoad;

    /// <summary>
    /// UI elements for dialogs to use.
    /// </summary>
    public DialogContext dialogContext;

    private BaseDialog currentDialog = null;

    /// <summary>
    /// A reference to the global DialogDatabase object.
    /// </summary>
    public static DialogsDatabase instance = null;

    private XmlDocument loadDocument(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(filename);
        if (textAsset == null)
        {
            throw new System.Exception("File \"" + filename + "\" not found.");
        }

        XmlDocument xmlDoc = new XmlDocument();

        xmlDoc.LoadXml(textAsset.text);

        return xmlDoc;
    }

    /// <summary>
    /// Loads dialog pairs (can be used for localization) from a XML file (located in Resources/Dialogs/Pairs/).
    /// </summary>
    /// <param name="filename">Filename (without extension).</param>
    public void loadFileOfPairs(string filename)
    {
        filename = "Dialogs/Pairs/" + filename;

        XmlDocument xmlDoc;
        try
        {
            xmlDoc = loadDocument(filename);
        }
        catch (XmlException e)
        {
            Debug.LogError("Unable to parse XML file. Error: " + e.Message);
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unable to parse XML file (not a XmlException). Error: " + e.Message);
            return;
        }

        // Parsowanie XMLi dla wartości dialogowych.
        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
        {
            dialogPairs.Add(node.Name, node.InnerText);
        }
    }

    /// <summary>
    /// Loads a dialog tree from a XML file (located in Resources/Dialogs/Trees/).
    /// </summary>
    /// <param name="filename">Filename (without extension).</param>
    public void loadFileOfDialogs(string filename)
    {
        filename = "Dialogs/Trees/" + filename;

        XmlDocument xmlDoc;

        try
        {
            xmlDoc = loadDocument(filename);
        }
        catch (XmlException e)
        {
            Debug.LogError("Unable to parse XML file. Error: " + e.Message);
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unable to parse XML file (not a XmlException). Error: " + e.Message);
            return;
        }

        BaseDialog beginNode = null;
        Dictionary<string, BaseDialog> registeredNodes = new Dictionary<string, BaseDialog>();

        // Parsowanie XMLi dla drzewek dialogowych.
        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
        {
            string type = node.Attributes.GetNamedItem("type").InnerText.ToLower();
            switch (type)
            {
                case "showdialog":
                    {
                        string who = node.Attributes.GetNamedItem("who")?.InnerText;
                        string whichNext = node.Attributes.GetNamedItem("next")?.InnerText;
                        string text = node.InnerText, finalText;
                        if (!dialogPairs.TryGetValue(text, out finalText))
                        {
                            finalText = text;
                        }

                        ShowDialog workingNode = new ShowDialog();
                        //workingNode.nextNode = registeredNodes[whichNext];
                        workingNode.who = who;
                        workingNode.dialog = text;
                        registeredNodes.Add(node.Name, workingNode);
                        if (beginNode == null)
                        {
                            beginNode = workingNode;
                        }
                    }
                    break;
                case "choicedialog":
                    {
                        string who = node.Attributes.GetNamedItem("who")?.InnerText;
                        string text = node.InnerText, finalText;
                        if (!dialogPairs.TryGetValue(text, out finalText))
                        {
                            finalText = text;
                        }

                        ChoiceDialog choiceDialog = new ChoiceDialog();
                        choiceDialog.who = who;
                        choiceDialog.dialog = text;
                        registeredNodes.Add(node.Name, choiceDialog);
                        if (beginNode == null)
                        {
                            beginNode = choiceDialog;
                        }
                    }
                    break;
                default:
                    {
                        Debug.LogWarning("Unknown dialog type: " + type);
                    }
                    break;
            }
        }

        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
        {
            string type = node.Attributes.GetNamedItem("type").InnerText.ToLower();
            switch (type)
            {
                case "showdialog":
                    {
                        string whichNext = node.Attributes.GetNamedItem("next")?.InnerText;
                        if (whichNext.Length > 0)
                        {
                            ShowDialog workingNode = (ShowDialog)registeredNodes[node.Name];
                            workingNode.nextNode = registeredNodes[whichNext];
                        }
                    }
                    break;
                case "choicedialog":
                    {
                        //int choices = int.Parse(node.Attributes.GetNamedItem("choices").InnerText);
                        int choices = (node.Attributes.Count - 2) / 2;
                        string[] choiceNodes = new string[choices], choiceStrs = new string[choices];
                        for (int x = 0; x < choices; ++x)
                        {
                            choiceStrs[x] = node.Attributes.GetNamedItem("choice_" + x).InnerText;
                            choiceNodes[x] = node.Attributes.GetNamedItem("choiceNode_" + x).InnerText;
                        }
                        ChoiceDialog workingNode = (ChoiceDialog)registeredNodes[node.Name];
                        workingNode.choices = choiceStrs;
                        workingNode.choiceNodes = new BaseDialog[choices];
                        for (int x = 0; x < choices; ++x)
                        {
                            workingNode.choiceNodes[x] = registeredNodes[choiceNodes[x]];
                        }
                    }
                    break;
            }
        }

        if (beginNode != null)
        {
            registeredDialogs.Add(filename, beginNode);
        }
    }

    private string showNode(BaseDialog node, out BaseDialog nextNode)
    {
        nextNode = null;
        string result = "";

        switch (node)
        {
            case ShowDialog showDialog:
                result = "Show \"" + showDialog.dialog + "\" from \"" + showDialog.who + "\".";
                nextNode = showDialog.nextNode;
                break;
        }

        return result;
    }

    /// <summary>
    /// Shows the given tree. Use only for debugging.
    /// </summary>
    /// <param name="treeName">Name of the file containing the tree.</param>
    /// <returns>Human readable string containing all of the tree's nodes.</returns>
    public string showTree(string treeName)
    {
        BaseDialog dialog = registeredDialogs["Dialogs/Trees/" + treeName];
        string result = "";

        while (dialog != null)
        {
            result += showNode(dialog, out dialog) + "\n";
        }

        return result;
    }

    void Start()
    {
        instance = this;
        dialogPairs = new Dictionary<string, string>();
        registeredDialogs = new Dictionary<string, BaseDialog>();
        foreach (string filename in filesOfPairsToLoad)
        {
            loadFileOfPairs(filename);
        }

        foreach (string filename in filesOfDialogsToLoad)
        {
            loadFileOfDialogs(filename);
        }

        PlayerController.playerInputActions.Dialogs.ProceedWithDialog.started += OnProceed;
    }

    private void OnProceed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (currentDialog != null)
        {
            processNextNode();
        }
    }

    /// <summary>
    /// Show the dialog tree.
    /// </summary>
    /// <param name="treeName">Dialog tree to show.</param>
    public void showDialog(string treeName)
    {
        currentDialog = registeredDialogs["Dialogs/Trees/" + treeName];
        currentDialog.process(dialogContext);
        PlayerController.instance.isMovementLocked = true;
    }

    /// <summary>
    /// Loads the next node (if the current node allows it) and prepares it (calls process).
    /// </summary>
    public void processNextNode()
    {
        if (currentDialog.isNextNodeAvailable)
        {
            currentDialog = currentDialog.nextNode;
            if (currentDialog != null)
            {
                currentDialog.process(dialogContext);
            }
            else
            {
                // Deactivate all of the dialog's UI elements and unlock the player controls.
                PlayerController.instance.isMovementLocked = false;
                dialogContext.dialogTextField.text = dialogContext.whoTextField.text = "";
                foreach (var butt in dialogContext.choiceButtons)
                {
                    butt.enabled = false;
                }
            }
        }
    }
}
