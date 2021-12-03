import React from 'react';
import { HashRouter as Router, Route, Switch, Redirect } from 'react-router-dom';
import { MasterDetail } from 'components';
import { MasterContainer, DetailContainer } from 'containers';
import { Owner } from 'Owner';
import { Repositories } from 'Repositories';
import { WorkflowRuns } from 'WorkflowRuns';
import { WorkflowRunAttempts } from 'WorkflowRunAttempts';
import { WorkflowRunAttempt } from 'WorkflowRunAttempt';

export const App = () => {
  return (
    <Router>
      <Switch>
        <Route path="/owner/:owner/:repo/:run/:attempt"
          render={props => (
            <WorkflowRunAttempt></WorkflowRunAttempt>
          )} />
        <Route path="/owner/:owner/:repo/:run"
          render={props => (
            <WorkflowRunAttempts></WorkflowRunAttempts>
          )} />
        <Route path="/owner/:owner/:repo"
          render={props => (
            <WorkflowRuns></WorkflowRuns>
          )} />
        <Route path="/owner/:owner"
          render={props => (
            <Repositories></Repositories>
          )} />
        <Route path="/owner"
          render={props => (
            <Owner></Owner>
          )} />
        <Route path="/master/:owner/:repo"
          render={props => (
            <MasterDetail MasterType={MasterContainer} masterProps={{}} 
                          DetailType={DetailContainer} detailProps={{}}/>
          )} />
        <Route path="/:page/:owner/:repo"
          render={props => (
            <MasterDetail MasterType={MasterContainer} masterProps={{}} 
                          DetailType={DetailContainer} detailProps={{}}/>
          )} />
        <Redirect exact from="/" to="/master/runner/server" />
      </Switch>
    </Router>
  );
};