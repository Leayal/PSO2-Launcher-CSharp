namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public class PatchRootInfoValue
    {
        public readonly string RawValue;
        public readonly bool IsNumber;
        public readonly int NumberValue; // Int for now

        public PatchRootInfoValue(in string rawValue)
        {
            this.RawValue = rawValue;
            
            // Int for now
            this.IsNumber = int.TryParse(rawValue, out this.NumberValue);
        }
    }
}
