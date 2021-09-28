using UnityEngine;

public class WaterNode
{
    Vector3 positionBase;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;

    #region Properties
        public Vector3 Displacement {
            get => position - positionBase;
        }
    #endregion

    #region Public Functions
        
        #region Construnctors
            public WaterNode(Vector3 position)
            {
                positionBase = position;
                this.position = position;
            }
            public WaterNode(Vector2 position)
            {
                positionBase = position;
                this.position = position;
            }
        #endregion

        public void Update(float SpringConstant, float Damping) 
        {
            acceleration = -SpringConstant * Displacement + velocity * Damping;   
            
            position += velocity;
            velocity += acceleration;
        }
    #endregion
}