#include "./DataStruct.cginc"

void AtomicAddDelta(int indexIntoDeltaPos, float newDeltaVal, int axis)
{
  uint i_val = asuint(newDeltaVal);
  uint tmp0 = 0;
  uint tmp1;

  [allow_uav_condition]
  while (true)
  {
    InterlockedCompareExchange(deltaPosUint[indexIntoDeltaPos][axis], tmp0, i_val, tmp1);

    if (tmp1 == tmp0) break;

    tmp0 = tmp1;
    i_val = asuint(newDeltaVal + asfloat(tmp1));
  }

  return;
}