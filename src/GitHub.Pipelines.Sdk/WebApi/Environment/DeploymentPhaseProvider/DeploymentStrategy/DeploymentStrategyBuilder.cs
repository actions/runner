using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal static class DeploymentStrategyBuilder
    {
        public static DeploymentStrategyBase Build(
            Dictionary<String, JToken> strategy,
            ValidationResult validationResult)
        {
            DeploymentStrategyBase deploymentStrategy = null;

            // If provided Jobject does not have anything return default empty RunOnce Strategy and add empty action
            if (strategy == null || !strategy.Any())
            {
                deploymentStrategy = new RunOnceDeploymentStrategy();
                deploymentStrategy.Actions.Add(new DeploymentStrategyDeployAction());
            }
            else
            {
                if (strategy.Count > 1)
                {
                    validationResult.Errors.Add(new PipelineValidationError("Deployment job does not support multiple strategies"));

                    return deploymentStrategy;
                }

                switch (strategy.FirstOrDefault().Key)
                {
                    case DeploymentStrategyConstants.DeploymentStrategyRunOnce:
                        deploymentStrategy = BuildRunOnceDeploymentStrategy(strategy.FirstOrDefault().Value, validationResult);
                        break;

                    case DeploymentStrategyConstants.DeploymentStrategyRolling:
                        deploymentStrategy = BuildRollingDeploymentStrategy(strategy.FirstOrDefault().Value, validationResult);
                        break;

                    default:
                        validationResult.Errors.Add(new PipelineValidationError($"Unexpected deployment strategy {strategy.FirstOrDefault().Key}"));
                        break;
                }
            }

            // Ensure at least on action should be defined
            if (deploymentStrategy != null && !deploymentStrategy.Actions.Any())
            {
                validationResult.Errors.Add(new PipelineValidationError("DeploymentStrategy should have at least one deploy action defined"));
            }

            return deploymentStrategy;
        }

        private static RunOnceDeploymentStrategy BuildRunOnceDeploymentStrategy(
            JToken runOnceStrategy,
            ValidationResult validationResult)
        {
            var strategy = new RunOnceDeploymentStrategy();

            var strategyProps = runOnceStrategy.Children<JProperty>();
            
            foreach (var prop in strategyProps)
            {
                switch (prop.Name)
                {
                     case DeploymentStrategyConstants.StrategyDeployActionName:
                         strategy.Actions.Add(BuildDeployAction(prop.Value, validationResult));
                         break;

                     default:
                        validationResult.Errors.Add(new PipelineValidationError($"Unexpected deployment action {prop.Name}"));
                        break;
                }
            }

            return strategy;
        }

        private static RollingDeploymentStrategy BuildRollingDeploymentStrategy(
            JToken rollingStrategy,
            ValidationResult validationResult)
        {
            var deploymentOption = DefaultDeploymentOption;
            var deploymentOptionValue = DefaultDeploymentOptionValue;

            var selector = new List<String>();
            DeploymentStrategyBaseAction action = null;

            var strategyProps = rollingStrategy.Children<JProperty>();

            foreach (var prop in strategyProps)
            {
                switch (prop.Name)
                {
                    case DeploymentStrategyConstants.StrategyDeployActionName:
                        action = BuildDeployAction(prop.Value, validationResult);
                        break;

                    case DeploymentStrategyConstants.RollingDeploymentMaxBatchSize:
                        TryToFillRollingOptions(prop.Value.ToString(), out deploymentOption, out deploymentOptionValue, validationResult);
                        break;

                    case DeploymentStrategyConstants.RollingDeploymentSelector:
                        selector.AddRange(prop.Value.ToString().Split(','));
                        break;

                    default:
                        validationResult.Errors.Add(new PipelineValidationError($"{prop.Name} is an unexpected property with rolling strategy"));
                        break;
                }
            }

            var strategy = new RollingDeploymentStrategy(deploymentOption, deploymentOptionValue, selector);
            if (action != null)
            {
                strategy.Actions.Add(action);
            }

            return strategy;
        }

        private static void TryToFillRollingOptions(
            String deploymentValue,
            out RollingDeploymentOption deploymentOption,
            out Int32 deploymentOptionValue,
            ValidationResult validationResult)
        {
            deploymentOptionValue = DefaultDeploymentOptionValue;
            deploymentOption = DefaultDeploymentOption;

            if (!String.IsNullOrWhiteSpace(deploymentValue))
            {
                if (deploymentValue.EndsWith("%"))
                {
                    var rollingOptionString = deploymentValue.TrimEnd('%');

                    if (Int32.TryParse(rollingOptionString, out var value) && value > 0 && value <= 100)
                    {
                        deploymentOption = RollingDeploymentOption.Percentage;
                        deploymentOptionValue = value;
                    }
                    else
                    {
                        validationResult.Errors.Add(new PipelineValidationError($"Percentage rolling option should be within 1 to 100. Provided maxBatchSize '{deploymentValue}' is not valid"));
                    }
                }
                else
                {
                    if (Int32.TryParse(deploymentValue, out var value) && value > 0)
                    {
                        deploymentOption = RollingDeploymentOption.Absolute;
                        deploymentOptionValue = value;
                    }
                    else
                    {
                        validationResult.Errors.Add(new PipelineValidationError($"Rolling value should be positive integer. Provided maxBatchSize '{deploymentValue}' is not valid"));
                    }
                }
            }
        }

        private static DeploymentStrategyBaseAction BuildDeployAction(
            JToken deployJToken,
            ValidationResult validationResult)
        {
            var deployAction = new DeploymentStrategyDeployAction();

            var stepsJProperty = deployJToken.Children<JProperty>().FirstOrDefault(x => x.Name == DeploymentStrategyConstants.StepsPropertyName);

            if (stepsJProperty != null && stepsJProperty.HasValues)
            {
                deployAction.Steps.AddRange(DeploymentStrategyTaskSteps.Build(stepsJProperty.Value.Children<JToken>(), validationResult));
            }

            return deployAction;
        }

        private const RollingDeploymentOption DefaultDeploymentOption = RollingDeploymentOption.Percentage;
        private const Int32 DefaultDeploymentOptionValue = 100;
    }
}
