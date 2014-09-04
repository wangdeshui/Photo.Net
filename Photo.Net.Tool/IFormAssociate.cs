using System.Windows.Forms;

namespace Photo.Net.Tool
{
    /// <summary>
    /// Used by classes to indicate they are associated with a certain Form, even if
    /// they are not contained within the Form. To this end, they are an Associate of
    /// the Form.
    /// </summary>
    public interface IFormAssociate
    {
        Form AssociatedForm { get; }
    }
}
