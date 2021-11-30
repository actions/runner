import React from 'react';
import { HashRouter as Router, Route, Switch, Redirect } from 'react-router-dom';
import { MasterDetail } from 'components';
import { MasterContainer, DetailContainer } from 'containers';

export const App = () => {
  return (
    <Router>
      <Switch>
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