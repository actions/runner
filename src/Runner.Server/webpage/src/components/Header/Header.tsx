import React, { ReactElement } from 'react';
import { Link } from 'react-router-dom';
import Media from 'react-media';
import { mediaQueries } from 'utils';
import styles from './Header.module.scss';
import { JsxElement } from 'typescript';

export interface HeaderProps {
    title: string,
    hideBackButton?: boolean,
    content?: ReactElement<any, any>
}

export const headerEmptyTitle = 'No Title';

export const Header: React.FC<HeaderProps> = (props) => {

    return (
        <div className={styles.header}>

            <Media query={mediaQueries.md}>
                { matches => matches ? (
                    <Link to="../../../../" className={styles.back}
                        style={{ visibility: props.hideBackButton ? 'hidden' : 'visible' }}>
                        Back
                    </Link>
                ): (
                    <div>&nbsp;</div>
                )}
            </Media>

            <h1 data-test="HeaderTitle">
                { props.title || headerEmptyTitle }
                { props.content }
            </h1>
        </div> 
    );
}

Header.defaultProps = {
    hideBackButton: false
};

export default Header;