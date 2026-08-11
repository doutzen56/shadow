---
name: add-payment-channel
description: >-
  Add a new recharge (代收) or withdraw (代付) external channel under
  ExternalChannels: models, core class, ChannelCoreKnownImplementations registry.
---

# add-payment-channel

1. Confirm 代收 vs 代付
2. Copy the closest existing `*PayInRecharge` / `*Withdraw` as template
3. Add Models → Core implementation → register in `ChannelCoreKnownImplementations`
4. List `hx_supplier` config items the human must set outside the repo (do not write prod secrets)
5. Suggest local `dotnet build` on touched projects
