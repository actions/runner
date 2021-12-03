import React, {useState,useEffect} from 'react';
import { useRouteMatch, Link, useLocation, useParams } from 'react-router-dom';
import { Header, ListItemLink } from 'components';
import { Items } from 'state';
import { ghHostApiUrl } from 'settings';

export interface OwnerProps {
}
interface IOwner {
    name: string,
}

export const Owner: React.FC<OwnerProps> = (props) => {
    let { url } = useRouteMatch();
    const [ owners, setOwners ] = useState<IOwner[]>([]);
    const { page } = useParams<{page: string}>();
    useEffect(() => {
        var apiUrl = ghHostApiUrl + "/_apis/v1/Message/owners?page=" + (page || "0");
        (async () => {
            var newOwners = JSON.parse((await (await fetch(apiUrl, { })).text())) as IOwner[];
            setOwners(_ => {
                return newOwners;
            });
        })();
    }, [page])
    const location = useLocation<string>();
    return (
        <React.Fragment>
            <Header title="Owner" hideBackButton={true} content={
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
                {owners.map((x: IOwner) =>
                    <li key={x.name}>
                        <ListItemLink 
                            to={`${url}/${encodeURIComponent(x.name)}`} item={{ id:  x.name, title: x.name, description: "" }} />
                    </li>
                )}
            </ul>
        </React.Fragment>
    );
};

