namespace CIT_BlastAwesomenessModifier
{
    public class ModuleBAMDebug : PartModule
    {
        [KSPField(guiActive = true)] public string BAMPower;

        [KSPEvent(name = "Explode", guiActive = true)]
        public void Explode()
        {
            this.part.explode();
        }

        internal void UpdatePower()
        {
            this.BAMPower = this.part.explosionPotential.ToString("F5");
        }
    }
}