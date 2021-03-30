import React from 'react'; 
import { NavLink } from 'react-router-dom';
import { ListItem, ListItemProps } from '../ListItem/ListItem';
import styles from './ListItemLink.module.scss';

export interface ListItemLinkProps extends ListItemProps {
    to: string
}

export const ListItemLink: React.FC<ListItemLinkProps> = (props) => {  
    return (
        <NavLink exact to={props.to}
            className={styles.component}
            activeClassName={styles.active}>
            <ListItem {...props} />
        </NavLink>
    );
};

export default ListItemLink;