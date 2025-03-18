namespace Game
{
    public interface IKccCharacterCollidable 
    {
        void OnCollisionEnter(KccCharacter character);
        void OnCollisionExit(KccCharacter character);
        void OnTriggerEnter(KccCharacter character);
        void OnTriggerExit(KccCharacter character);
    }
}
