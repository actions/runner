# BYOS: Bring Your Own Subscription Agent Pools

BYOS will hydrate and manage a set of build and release agents completely automated in the user's azure subscription.  

BYOS will be in the middle of the cost and convenience spectrum between hosted and private while offering most of the control of private pools.

## State

This is in the early design phase and we are looking for feedback.  Feedback can be provided as issues in this repo with the enhancement and design tags.

## Goals

- **Fully automated dedicated agents with elasticity**: User configures contraints and we provision, start and stop the agents.
- **Control of image and toolsets**: Pick the image to use.  Stay on it until you change the configuration.  Use our published images that we release monthly.
- **Control machine configurations**: User can provide VM sku and other configuration options (provide ARM).
- **Control agent lifetime**: Builds can be single use, or thrown away on a configured interval (nightly, etc).
- **Incremental sources and packages**: Even if you choose single use.  Warm up yaml run when bringing VM online. 
- **Control azure COGs**: Stop agents when not in use to control azure charges
- **Maintenance**: Schedule maintenance jobs for pruning repos, OS security updates, etc.
- **Elastic pools for VSTS and On-prem**:  Use elastic azure compute as build resources for VSTS but also on-prem TFS.
- **Allow domain joined and on-prem file shares**: Leverage AAD and express routes for elastic on-prem scenarios.
- **Configure multiple pools of type BYOS**: Allows for budgeting of resources across larger enterprise teams.

## Design

Pending on goals discussions.


