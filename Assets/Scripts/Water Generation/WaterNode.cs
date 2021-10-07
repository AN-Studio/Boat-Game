using UnityEngine;

public partial class WaterGenerator
{
    public class WaterNode
    {
        Vector2 positionBase;
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 acceleration;
        public Vector2 disturbance;

        // const float massPerNode = 0.04f;

        #region Properties
            public Vector2 Displacement {
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

            public void Update(float springConstant, float damping, float massPerNode) 
            {
                Vector2 force = springConstant * Displacement + velocity * damping;   
                acceleration = -force / massPerNode + disturbance * Time.fixedDeltaTime;
                disturbance += -disturbance * damping;

                position += velocity * Time.fixedDeltaTime;
                velocity += acceleration;
            }
            public void Splash(Vector2 momentum, float massPerNode) {
                momentum.y = Mathf.Min(0f, momentum.y);
                this.velocity += momentum / massPerNode * Time.fixedDeltaTime;
            }
            public void Disturb(Vector2 acceleration){
                disturbance += acceleration;
            }
        #endregion
    }

}