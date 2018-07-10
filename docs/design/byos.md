# BYOS: Bring Your Own Subscription Agent Pools

BYOS pairs the convenience and elastic capacity of the hosted pool with the control and flexibility of private agents.
VSTS will manage a set of build and release agents to the customer's specification, completely automated, in the customer's Azure subscription.  
BYOS will be in the middle of the cost vs. convenience spectrum between hosted and private.

## State

This is in the early design phase and we are looking for feedback.  Feedback can be provided as issues in this repo with the enhancement and design tags.

## Goals

- **Fully automated dedicated agents with elasticity**: User configures contraints and we provision, start and stop the agents.
- **Customer control of image and toolsets**: Pick the image to use.  Stay on it until you change the configuration.  Use our published images that we release monthly.
- **Control machine configurations**: User can provide VM SKU and other configuration options (provide ARM).
- **Control agent lifetime**: Agents can be single use, or thrown away on a configured interval (nightly, etc).
- **Incremental sources and packages**: Even if you choose single use, we can warm up YAML run when bringing VM online. 
- **Cached container images on host**: Ensure a set of container images are cached on the host via warmup YAML.
- **Maintenance**: Schedule maintenance jobs for pruning repos, OS security updates, etc.
- **Elastic pools for VSTS and On-prem**:  Use elastic Azure compute as build resources for VSTS but also on-prem TFS.
- **Allow domain joined and on-prem file shares**: Leverage AAD and Express Routes for elastic on-prem scenarios.
- **Configure multiple pools of type BYOS**: Allows for budgeting of resources across larger enterprise teams.
- **Control costs**: Stop agents when not in use to control Azure charges

## Design

Pending on goals discussions.


