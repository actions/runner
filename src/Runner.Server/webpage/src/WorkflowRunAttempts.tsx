import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';

export interface WorkflowRunAttemptsProps {
}
interface IWorkflowRun {
    attempt: string,
    fileName: string
}

export const WorkflowRunAttempts: React.FC<WorkflowRunAttemptsProps> = (props) => {
    let { url } = useRouteMatch();
    const [ owners, setOwners ] = useState<IWorkflowRun[]>([]);
    const { page, owner, repo, run } = useParams<{page: string, owner: string, repo: string, run: string}>();
    useEffect(() => {
        var apiUrl = `${ghHostApiUrl}/_apis/v1/Message/workflow/run/${run}/attempts?owner=${encodeURIComponent(owner)}&repo=${encodeURIComponent(repo)}&page=${page || "0"}`;
        (async () => {
            var newOwners = JSON.parse((await (await fetch(apiUrl, { })).text())) as IWorkflowRun[];
            setOwners(_ => {
                return newOwners;
            });
        })();
    }, [page, owner, repo])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title={`WorkflowRunAttempts ${owner}/${repo}`} hideBackButton={false} content={
                <div>
                    <Link to={location.pathname.replace("/" + (page || "master"), "/" + (Number.parseInt(page || "0") - 1))} style={{ visibility: location.pathname.startsWith('/master/') || location.pathname === '/master' || location.pathname.startsWith('/0/') || location.pathname === '/0' ? 'hidden' : 'visible' }}>
                        Previous
                    </Link>
                    <Link to={location.pathname.replace("/" + (page || "master"), "/" + (Number.parseInt(page || "0") + 1))} >
                        Next
                    </Link>
                </div>
            }/>
            <ul>
                {owners.map((x: IWorkflowRun) =>
                    <li key={x.attempt}>
                        <ListItemLink 
                            to={`${url}/${encodeURIComponent(x.attempt)}`} item={{ id:  x.attempt, title: x.attempt, description: x.fileName }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

