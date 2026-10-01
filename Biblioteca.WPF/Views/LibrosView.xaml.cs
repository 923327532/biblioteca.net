using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Biblioteca.WPF.Views;

public partial class LibrosView : UserControl
{
    private static readonly Regex DigitoOGuionRegex = new(@"^[0-9\-]+$");
    private static readonly Regex IsbnCompletoRegex = new(@"^\d{3}-\d{10}$");

    public LibrosView()
    {
        InitializeComponent();
        DataObject.AddPastingHandler(TxtIsbn, TxtIsbn_Pasting);
    }

    private void TxtIsbn_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        // 1. Bloquear cualquier carácter que no sea dígito ni guion (no se permite texto/letras)
        if (!DigitoOGuionRegex.IsMatch(e.Text))
        {
            e.Handled = true;
            return;
        }

        // 2. Si se escribe un guion manualmente, solo se permite en la posición 3 (tras 3 dígitos) y si no hay otro guion
        if (e.Text == "-")
        {
            if (textBox.CaretIndex != 3 || textBox.Text.Contains('-'))
            {
                e.Handled = true;
                return;
            }
            return;
        }

        // 3. Si se escribe el 4to dígito habiendo 3 dígitos sin guion, auto-insertar el guion junto con el dígito
        if (textBox.CaretIndex == 3 && !textBox.Text.Contains('-') && textBox.SelectionLength == 0)
        {
            int caret = textBox.CaretIndex;
            string newText = textBox.Text.Insert(caret, "-" + e.Text);
            if (newText.Length <= 14)
            {
                textBox.Text = newText;
                textBox.CaretIndex = caret + 2;
                e.Handled = true;
                return;
            }
        }

        // 4. Prevenir que se superen los 14 caracteres máximos
        string currentText = textBox.Text ?? string.Empty;
        int selStart = textBox.SelectionStart;
        int selLen = textBox.SelectionLength;
        string resultingText = currentText.Remove(selStart, selLen).Insert(selStart, e.Text);

        if (resultingText.Length > 14)
        {
            e.Handled = true;
            return;
        }
    }

    private void TxtIsbn_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string clipboardText = (e.DataObject.GetData(DataFormats.Text) as string)?.Trim() ?? string.Empty;

        // Rechazar si contiene caracteres que no sean dígitos o guiones
        if (string.IsNullOrWhiteSpace(clipboardText) || !clipboardText.All(c => char.IsDigit(c) || c == '-'))
        {
            e.CancelCommand();
            return;
        }

        // Si se pegaron 13 dígitos numéricos sin guion, formatear automáticamente con el guion
        if (clipboardText.Length == 13 && clipboardText.All(char.IsDigit))
        {
            clipboardText = $"{clipboardText[..3]}-{clipboardText[3..]}";
        }

        // Si el resultado cumple con el formato exacto de 14 caracteres: '978-8437604909'
        if (IsbnCompletoRegex.IsMatch(clipboardText))
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = clipboardText;
                textBox.CaretIndex = clipboardText.Length;
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }
}
